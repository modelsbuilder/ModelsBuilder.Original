using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ChristianHelle.DeveloperTools.CodeGenerators.Resw.VSPackage.CustomTool
{
    public class ResourceParser : IResourceParser
    {
        public ResourceParser(string reswFileContents)
        {
            ReswFileContents = reswFileContents;
        }

        public string ReswFileContents { get; set; }

        public List<ResourceItem> Parse()
        {
            var doc = XDocument.Parse(ReswFileContents);

            var list = new List<ResourceItem>();

            foreach (var element in doc.Descendants("data"))
            {
                if (element.Attributes().All(c => c.Name != "name"))
                    continue;

                var item = new ResourceItem();

                var nameAttribute = element.Attribute("name");
                if (nameAttribute != null)
                    item.Name = nameAttribute.Value;

                if (element.Descendants().Any(c => c.Name == "value"))
                {
                    var valueElement = element.Descendants("value").FirstOrDefault();
                    if (valueElement != null)
                        item.Value = valueElement.Value;
                }

                if (element.Descendants().Any(c => c.Name == "comment"))
                {
                    var commentElement = element.Descendants("comment").FirstOrDefault();
                    if (commentElement != null)
                        item.Comment = commentElement.Value; 
                }

                list.Add(item);
            }

            return list;
        }
    }
}
