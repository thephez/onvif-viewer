using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSP_Viewer.Classes
{
    class TagDatabase
    {
        public List<Tag> Tags = new List<Tag>();

        /// <summary>
        /// Add a tag by a string with an option to set the value field
        /// </summary>
        /// <param name="tagname">Name of tag to add</param>
        /// <param name="value">Value to assign to tag</param>
        public void AddTag(string tagname, string value = null)
        {
            Tag newTag = new Tag();
            newTag.name = tagname;
            newTag.value = value;

            if (!Tags.Exists(x => x.name == newTag.name))
            {
                Tags.Add(newTag);
            }
            else
            {
                Debug.Print(string.Format("Tag {0} already exists.  Skipping AddTag", newTag.name));
            }
        }

        /// <summary>
        ///  Update a tag value (ignores unchanged values) 
        /// </summary>
        /// <param name="tagname">Name of tag to update</param>
        /// <param name="value">Value to assign to tag</param>
        /// <returns>True if tag updated / false if tag not updated</returns>
        public bool UpdateTagValue(string tagname, string value)
        {
            Tag newTag = new Tag();
            newTag.name = tagname;
            newTag.value = value;

            if (!Tags.Exists(x => x.name == newTag.name))
            {
                AddTag(tagname, value);
                Debug.Print(string.Format("Tag {0} does not exist locally.  Adding to local Tag DB", newTag.name));
                return false;
            }
            else
            {
                int index = Tags.FindIndex(t => t.name == newTag.name);

                if (Tags[index].value != value)
                {
                    Tags[index].value = value;
                    Tags[index].timestamp = DateTime.Now.ToString();
                    //Debug.Print(String.Format("{0} set to {1}", tagname, value))
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
    
    /// <summary>
    /// Basic Tag structure. Supports notification for changes of the value property
    /// </summary>
    public class Tag //: System.ComponentModel.INotifyPropertyChanged
    {
        //private _value;
        // Basic Tag Properties
        public string name { get; set; }
        public string value { get; set; }
        //{
        //get { return _value; }
        //set
        //{
        //    _value = value;
        //    if (PropertyChanged != null)
        //    {
        //        PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("value"));
        //    }
        //}
        //}

        public string quality { get; set; }
        //{ Get; Set; }
        public string timestamp { get; set; }
        //{ Get; Set; }
        public string alrstatus { get; set; }
        //{ Get; Set; }
        public string ack { get; set; }
        //{ Get; Set; }
        public bool bindData { get; set; }

        //public event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged;
    }
}
