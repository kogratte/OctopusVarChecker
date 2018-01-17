using System;
using System.Collections.Generic;
using System.Configuration;

namespace OctopusVarChecker.Config
{
    public class OctopusProjectsSection: ConfigurationSection
    {
        [ConfigurationProperty("projects")]
        public OctopusProjectCollection Projects
        {
            get
            {
                return (OctopusProjectCollection)this["projects"];
            }
        }

        [ConfigurationProperty("apiKey", IsRequired = false)]
        public string ApiKey
        {
            get
            {
                return (string) this["apiKey"];
            }
            set
            {
                this["apiKey"] = value;
            }
        }

        [ConfigurationProperty("host", IsRequired = false)]
        public string Host
        {
            get
            {
                return (string)this["host"];
            } set
            {
                this["host"] = value;
            }
        }
    }

    [ConfigurationCollection(typeof(OctopusProject))]
    public class OctopusProjectCollection: ConfigurationElementCollection, IEnumerable<OctopusProject>
    {
        public OctopusProject this[int index]
        {
            get { return (OctopusProject)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);

                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new OctopusProject();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((OctopusProject)element).Name;
        }

        IEnumerator<OctopusProject> IEnumerable<OctopusProject>.GetEnumerator()
        {
            foreach (var key in this.BaseGetAllKeys())
            {
                yield return (OctopusProject)BaseGet(key);
            }
        }
    }

    public class OctopusProject: ConfigurationElement
    {
        /// <summary>
        /// Le nom du projet sur Octopus
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Le chemin vers le projet
        /// </summary>
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get
            {
                return (string)this["path"];
            }
            set
            {
                this["path"] = value;
            }
        }
    }
}
