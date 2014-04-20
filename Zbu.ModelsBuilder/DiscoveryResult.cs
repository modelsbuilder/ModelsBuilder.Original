using System;
using System.Collections.Generic;
using System.Linq;

namespace Zbu.ModelsBuilder
{
    public class DiscoveryResult
    {
        // "alias" is the umbraco alias
        // content "name" is the complete name eg Foo.Bar.Name
        // property "name" is just the local name

        private readonly HashSet<string> _ignoredContent 
            = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);  
        private readonly Dictionary<string, string> _renamedContent 
            = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _ignoredProperty
            = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, Dictionary<string, string>> _renamedProperty
            = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, string> _contentBase
            = new Dictionary<string, string>();
        private readonly Dictionary<string, string[]> _contentInterfaces 
            = new Dictionary<string, string[]>();

        #region Declare

        // content with that alias should not be generated
        public void SetIgnoredContent(string contentAlias)
        {
            _ignoredContent.Add(contentAlias);
        }

        // content with that alias should be generated with a different name
        public void SetRenamedContent(string contentAlias, string contentName)
        {
            _renamedContent[contentAlias] = contentName;
        }

        // property with that alias should not be generated
        // applies to content name and any content that implements it
        // here, contentName may be an interface
        public void SetIgnoredProperty(string contentName, string propertyAlias)
        {
            HashSet<string> ignores;
            if (!_ignoredProperty.TryGetValue(contentName, out ignores))
                ignores = _ignoredProperty[contentName] = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            ignores.Add(propertyAlias);
        }

        // property with that alias should be generated with a different name
        // applies to content name and any content that implements it
        // here, contentName may be an interface
        public void SetRenamedProperty(string contentName, string propertyAlias, string propertyName)
        {
            Dictionary<string, string> renames;
            if (!_renamedProperty.TryGetValue(contentName, out renames))
                renames = _renamedProperty[contentName] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            renames[propertyAlias] = propertyName;
        }

        // content with that name has a base class so no need to generate one
        public void SetContentBaseClass(string contentName, string baseName)
        {
            _contentBase[contentName] = baseName;
        }

        // content with that name implements the interfaces
        public void SetContentInterfaces(string contentName, IEnumerable<string> interfaceNames)
        {
            _contentInterfaces[contentName] = interfaceNames.ToArray();
        }

        #endregion

        #region Query

        public bool IsContentIgnored(string contentAlias)
        {
            return _ignoredContent.Contains(contentAlias);
        }

        public bool HasContentBase(string contentAlias)
        {
            var contentName = ContentName(contentAlias);
            return _contentBase.ContainsKey(contentName);
        }

        public string ContentName(string contentAlias)
        {
            string name;
            return (_renamedContent.TryGetValue(contentAlias, out name)) ? name : contentAlias;
        }

        public bool IsPropertyIgnored(string contentAlias, string propertyAlias)
        {
            var contentName = ContentName(contentAlias);
            return IsPropertyIgnoredByName(contentName, propertyAlias);
        }

        private bool IsPropertyIgnoredByName(string contentName, string propertyAlias)
        {
            HashSet<string> ignores;
            if (_ignoredProperty.TryGetValue(contentName, out ignores)
                && ignores.Contains(propertyAlias)) return true;
            string baseName;
            if (_contentBase.TryGetValue(contentName, out baseName)
                && IsPropertyIgnoredByName(baseName, propertyAlias)) return true;
            string[] interfaceNames;
            if (_contentInterfaces.TryGetValue(contentName, out interfaceNames)
                && interfaceNames.Any(interfaceName => IsPropertyIgnoredByName(interfaceName, propertyAlias))) return true;
            return false;
        }

        public string PropertyName(string contentAlias, string propertyAlias)
        {
            var contentName = ContentName(contentAlias);
            return PropertyNameByName(contentName, propertyAlias) ?? propertyAlias;
        }

        public string PropertyNameByName(string contentName, string propertyAlias)
        {
            Dictionary<string, string> renames;
            string name;
            if (_renamedProperty.TryGetValue(contentName, out renames)
                && renames.TryGetValue(propertyAlias, out name)) return name;
            string baseName;
            if (_contentBase.TryGetValue(contentName, out baseName)
                && null != (name = PropertyNameByName(baseName, propertyAlias))) return name;
            string[] interfaceNames;
            if (_contentInterfaces.TryGetValue(contentName, out interfaceNames)
                && null != (name = interfaceNames
                    .Select(interfaceName => PropertyNameByName(interfaceName, propertyAlias))
                    .FirstOrDefault(x => x != null))) return name;
            return null;
        }

        #endregion
    }
}