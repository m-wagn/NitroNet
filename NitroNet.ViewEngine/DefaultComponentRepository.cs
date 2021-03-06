﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NitroNet.Common.Exceptions;

namespace NitroNet.ViewEngine
{
	public class DefaultComponentRepository : IComponentRepository
	{
		private readonly ITemplateRepository _templateRepository;
		private IEnumerable<FileTemplateInfo> _templatesOriginal;
		private Dictionary<string, ComponentDefinition> _templatesCached;

        public DefaultComponentRepository(ITemplateRepository templateRepository)
		{
			_templateRepository = templateRepository;
        }

		private Dictionary<string, ComponentDefinition> GetComponents()
		{
            var templates = _templateRepository.GetAll().Cast<FileTemplateInfo>();

            var refresh = !ReferenceEquals(templates, _templatesOriginal);
		    // ReSharper disable once PossibleMultipleEnumeration
			_templatesOriginal = templates;

		    if (refresh)
		    {
                _templatesCached = new Dictionary<string, ComponentDefinition>();

		        var filteredTemplates = templates
		            .Where(t => t.Type.Equals(TemplateType.Component))
		            .GroupBy(t => Path.GetDirectoryName(t.Path.ToString()))
		            .Select(CreateComponentDefinition);

		        var duplicates = filteredTemplates.GroupBy(x => x.Id).Where(group => group.Count() > 1);

                // Throw an error if there are duplicates in the component repository
		        if (duplicates.Any())
		        {
                    string pathsString = string.Empty;
                    var duplicateTemplates = duplicates.SelectMany(group => group);

                    foreach (var templ in duplicateTemplates)
                    {
                        pathsString += "_id: " + templ.Id + " _path: " + templ.DefaultTemplate.Path + Environment.NewLine;
                    }

                    throw new NitroNetComponentException("The following duplicate ids in the template repository were found: " + Environment.NewLine + pathsString);
		        }

                _templatesCached = _templatesCached.Concat(filteredTemplates.ToDictionary(i => i.Id, i => i)).ToDictionary(i => i.Key, i => i.Value);
            }

			return _templatesCached;
		}

		public IEnumerable<ComponentDefinition> GetAll()
		{
			return GetComponents().Values;
		}

		private static ComponentDefinition CreateComponentDefinition(IGrouping<string, FileTemplateInfo> t)
		{
			var componentId = GetComponentId(t.Key);
			var defaultTemplateCandidates = GetDefaultTemplateCandidates(componentId);
		    FileTemplateInfo defaultTemplate = null;
		    foreach (var defaultTemplateCandidate in defaultTemplateCandidates)
		    {
                defaultTemplate = t.FirstOrDefault(a => a.Path.ToString().EndsWith(defaultTemplateCandidate, StringComparison.InvariantCultureIgnoreCase));
                if (defaultTemplate != null)
		        {
		            break;
		        }
		    }
			
			var templates = t.ToList();

			if (defaultTemplate == null && templates.Count == 1)
				defaultTemplate = templates[0];

			var skins = templates.Where(t1 => t1 != defaultTemplate).ToDictionary(GetSkinName);
			if (defaultTemplate == null && skins.TryGetValue(string.Empty, out defaultTemplate))
				skins.Remove(string.Empty);

			return new ComponentDefinition(componentId, defaultTemplate, skins);
		}

        private static IEnumerable<string> GetDefaultTemplateCandidates(string componentId)
        {
            // todo extension should be configurable or ignored
            yield return string.Concat(componentId, '/', "default.html");
            yield return string.Concat(componentId, '/', "default.hbs");
            var fileName = Path.GetFileName(componentId);
            if (!string.IsNullOrEmpty(fileName))
            {
                yield return string.Concat(componentId, '/', fileName.Replace("-", string.Empty), ".html");
                yield return string.Concat(componentId, '/', fileName, ".html");
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                yield return string.Concat(componentId, '/', fileName.Replace("-", string.Empty), ".hbs");
                yield return string.Concat(componentId, '/', fileName, ".hbs");
            }
        }

        private static string GetComponentId(string componentId)
		{
		    var normalizedPath = componentId.Replace('\\', '/');
		    var pathSegments = normalizedPath.Split('/');
		    return pathSegments[pathSegments.Length - 1];
		}

		private static string GetSkinName(FileTemplateInfo templateInfo)
		{
			return templateInfo.Name;
		}

		public Task<ComponentDefinition> GetComponentDefinitionByIdAsync(string id)
		{
			var components = GetComponents();

			if (components.ContainsKey(id))
				return Task.FromResult(components[id]);

			return Task.FromResult<ComponentDefinition>(null);
		}
	}
}
