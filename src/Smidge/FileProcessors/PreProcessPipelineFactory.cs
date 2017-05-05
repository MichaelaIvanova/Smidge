﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Smidge.Models;
using System.Linq;
using Smidge.Options;

namespace Smidge.FileProcessors
{
    /// <summary>
    /// Defines the default pre-processor pipelines used
    /// </summary>
    public class PreProcessPipelineFactory
    {
        private readonly Lazy<IReadOnlyCollection<IPreProcessor>> _allProcessors;
        private Func<WebFileType, PreProcessPipeline, PreProcessPipeline> _setGetDefaultCallback;
        private readonly Dictionary<WebFileType, PreProcessPipeline> _default = new Dictionary<WebFileType, PreProcessPipeline>();

        public PreProcessPipelineFactory(Lazy<IEnumerable<IPreProcessor>> allProcessors)
        {
            _allProcessors = new Lazy<IReadOnlyCollection<IPreProcessor>>(() => allProcessors.Value.ToList());
        }

        /// <summary>
        /// Resolves an instance of a pre processor based on type
        /// </summary>
        /// <param name="preProcessorType"></param>
        /// <returns></returns>
        public IPreProcessor Resolve(Type preProcessorType)
        {
            return _allProcessors.Value.FirstOrDefault(x => x.GetType() == preProcessorType);
        }

        /// <summary>
        /// Returns a pipeline with the specified types in order
        /// </summary>
        /// <param name="preProcessorTypes"></param>
        /// <returns></returns>
        public PreProcessPipeline Create(params Type[] preProcessorTypes)
        {
            var processors = new List<IPreProcessor>();
            foreach (var type in preProcessorTypes)
            {
                processors.Add(_allProcessors.Value.First(x => x.GetType() == type));
            }
            return new PreProcessPipeline(processors);
        }

        /// <summary>
        /// Returns the default pipeline for a given file
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns></returns>
        public virtual PreProcessPipeline CreateDefault(WebFileType fileType)
        {
            var d = GetDefault(fileType).Copy();

            //try to use the callback first and if something is returned use it, otherwise use the defaults
            var result = _setGetDefaultCallback?.Invoke(fileType, d);

            return result ?? d;
        }

        private PreProcessPipeline GetDefault(WebFileType fileType)
        {
            if (_default.TryGetValue(fileType, out PreProcessPipeline pipeline))
                return pipeline;

            switch (fileType)
            {
                case WebFileType.Js:
                    _default[fileType] = new PreProcessPipeline(new IPreProcessor[]
                    {
                        _allProcessors.Value.OfType<JsMinifier>().First()
                    });
                    break;
                case WebFileType.Css:
                default:
                    _default[fileType] = new PreProcessPipeline(new IPreProcessor[]
                    {
                        _allProcessors.Value.OfType<CssImportProcessor>().First(),
                        _allProcessors.Value.OfType<CssUrlProcessor>().First(),
                        _allProcessors.Value.OfType<CssMinifier>().First()
                    });
                    break;
            }

            return _default[fileType];
        }

        /// <summary>
        /// Allows setting the callback used to get the default PreProcessPipeline, if the callback returns null
        /// then the logic defers to the CreateDefault default result
        /// </summary>
        public Func<WebFileType, PreProcessPipeline, PreProcessPipeline> OnCreateDefault
        {
            set { _setGetDefaultCallback = value; }
        }
    }
}