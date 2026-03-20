using System.Linq;
using System.Xml.Linq;

namespace Mnema.Common.Extensions;

public static class XmlExtensions
{
    extension(XElement parent)
    {
        /// <summary>
        /// Gets an existing element or creates it if missing.
        /// </summary>
        public XElement GetOrCreateElement(XName name)
        {
            var el = parent.Element(name);
            if (el == null)
            {
                el = new XElement(name);
                parent.Add(el);
            }
            return el;
        }

        /// <summary>
        /// Sets the value of an element. If the element doesn't exist, it creates it.
        /// </summary>
        public XElement SetOrAddElementValue(XName name, string? value)
        {
            var el = parent.GetOrCreateElement(name);
            el.Value = value ?? string.Empty;
            return el;
        }

        /// <summary>
        /// Finds or creates a meta tag with a specific property attribute.
        /// </summary>
        public XElement GetOrCreateMeta(XNamespace ns, string property, string? refines = null)
        {
            var el = parent.Elements().FirstOrDefault(e =>
                e.Attribute("property")?.Value == property &&
                (refines == null || e.Attribute("refines")?.Value == refines));

            if (el == null)
            {
                el = new XElement(ns + "meta", new XAttribute("property", property));
                if (!string.IsNullOrEmpty(refines))
                    el.Add(new XAttribute("refines", refines));

                parent.Add(el);
            }
            return el;
        }

        /// <summary>
        /// Updates or adds a meta property that "refines" a parent element via ID.
        /// </summary>
        public void SetRefinedMetadata(XNamespace ns, string property, string targetId, string? value)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            var meta = parent.GetOrCreateMeta(ns, property, $"#{targetId}");
            meta.Value = value ?? string.Empty;
        }
    }
}
