using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RTSP_Viewer.Test
{
    [TestClass]
    public class RTSP_ViewerTest
    {
        [TestMethod]
        public void Test_GetNumberOfViews()
        {
            //Arrange
            Viewer v = new Viewer();

            // Act
            int actual = v.GetNumberOfViews();
            
            //Assert
            Assert.IsNotNull(actual);
        }
    }
}
