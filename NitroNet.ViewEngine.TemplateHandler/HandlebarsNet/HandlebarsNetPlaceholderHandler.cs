﻿using HandlebarsDotNet.Compiler;
using Newtonsoft.Json.Linq;
using NitroNet.ViewEngine.ViewEngines.HandlebarsNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace NitroNet.ViewEngine.TemplateHandler.HandlebarsNet
{
    public class HandlebarsNetPlaceholderHandler : IHandlebarsNetHelperHandler
    {
        private readonly INitroTemplateHandler _handler;

        public HandlebarsNetPlaceholderHandler(INitroTemplateHandler handler)
        {
            _handler = handler;
        }

        public void Evaluate(TextWriter output, dynamic context, params object[] parameters)
        {
            var parametersAsDictionary = (HashParameterDictionary)parameters.First();

            var key = parametersAsDictionary["name"].ToString().Trim('"', '\'');
            var index = TryGetIndex(parametersAsDictionary, context);

            var viewContext = Sitecore.Mvc.Common.ContextService.Get().GetCurrent<ViewContext>();
            viewContext.Writer = output;
            _handler.RenderPlaceholder(context, key, index, output, viewContext);
        }

        private static string TryGetIndex(HashParameterDictionary parameters, object model)
        {
            string fullIndex = null;

            object index;
            if (parameters.TryGetValue("index", out index))
            {
                fullIndex = index.ToString().Trim('"', '\'');
            }

            object indexProperty;
            if (parameters.TryGetValue("indexprop", out indexProperty))
            {
                indexProperty = indexProperty.ToString().Trim('"', '\'');

                object indexPropertyValue;
                if (TryGetPropValue(model, indexProperty.ToString(), out indexPropertyValue))
                {
                    if (fullIndex == null)
                        return indexPropertyValue.ToString();

                    fullIndex += "_" + indexPropertyValue;
                }
            }

            return fullIndex;
        }

        private static bool TryGetPropValue<TValue>(object src, string propertyName, out TValue value)
        {
            value = default(TValue);

            //JObject
            var jObject = src as JObject;
            JToken jValue;
            if (jObject != null && jObject.TryGetValue(propertyName, StringComparison.InvariantCultureIgnoreCase, out jValue))
            {
                value = jValue.Value<TValue>();
                return true;
            }

            //Dictionary
            var dictionaryObject = src as IDictionary<string, object>;
            object dictionaryValue;
            if (dictionaryObject != null && dictionaryObject.TryGetValue(propertyName, out dictionaryValue) && dictionaryValue is TValue)
            {
                value = (TValue)dictionaryValue;
                return true;
            }

            var property = src.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                value = (TValue)property.GetValue(src, null);

                return true;
            }

            return false;
        }
    }
}
