using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RTSP_Viewer.Classes;

namespace RTSP_Viewer.Test
{
    [TestClass]
    public class TagDatabaseTest
    {
        [TestMethod]
        public void Test_AddTagWithValue()
        {
            //Arrange
            TagDatabase tagdb = new TagDatabase();
            string tagname = "test";
            string tagvalue = "1";
            Tag tag = new Tag() { name = tagname };

            // Act
            tagdb.AddTag(tagname, tagvalue);

            //Assert
            if (!tagdb.Tags.Exists(x => x.name == tag.name))
            {
                Assert.Fail(string.Format("Tag [{0}] was not added successfully", tagname));
            }
        }

        [TestMethod]
        public void Test_AddTagWithNullValue()
        {
            //Arrange
            TagDatabase tagdb = new TagDatabase();
            string tagname = "test";
            string tagvalue = null;
            Tag tag = new Tag() { name = tagname };

            // Act
            tagdb.AddTag(tagname, tagvalue);

            //Assert
            if (!tagdb.Tags.Exists(x => x.name == tag.name))
            {
                Assert.Fail(string.Format("Tag [{0}] was not added successfully", tagname));
            }
        }

        [TestMethod]
        public void Test_AddTagWithNoValue()
        {
            //Arrange
            TagDatabase tagdb = new TagDatabase();
            string tagname = "test";
            Tag tag = new Tag() { name = tagname };

            // Act
            tagdb.AddTag(tagname);

            //Assert
            if (!tagdb.Tags.Exists(x => x.name == tag.name))
            {
                Assert.Fail(string.Format("Tag [{0}] was not added successfully", tagname));
            }
        }

        /// <summary>
        /// Seems like this should actually fail
        /// </summary>
        [TestMethod]
        public void Test_AddTagWithNullName()
        {
            //Arrange
            TagDatabase tagdb = new TagDatabase();
            string tagname = null;
            Tag tag = new Tag() { name = tagname };

            // Act
            tagdb.AddTag(tagname);

            //Assert
            if (!tagdb.Tags.Exists(x => x.name == tag.name))
            {
                Assert.Fail(string.Format("Tag [{0}] was not added successfully", tagname));
            }
        }
    }
}
