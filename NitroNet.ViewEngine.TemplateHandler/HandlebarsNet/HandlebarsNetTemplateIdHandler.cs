﻿using HandlebarsDotNet.Compiler;
using NitroNet.ViewEngine.ViewEngines.HandlebarsNet;
using System.IO;
using System.Linq;

namespace NitroNet.ViewEngine.TemplateHandler.HandlebarsNet
{
    public class HandlebarsNetTemplateIdHandler : IHandlebarsNetHelperHandler
    {
        public void Evaluate(TextWriter output, dynamic context, params object[] parameters)
        {
            var parametersAsDictionary = (HashParameterDictionary)parameters.First();
            var templateId = parametersAsDictionary["templateId"];
            output.Write(templateId as string);
        }
    }
}