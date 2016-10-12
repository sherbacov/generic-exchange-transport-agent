using System.IO;
using System.Linq;
using Microsoft.Exchange.Data.Transport.Email;
using NUnit.Framework;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.Common.Impl;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.Common.Impl.Extensions;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.ExtractAttachmentHandler.Impl;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.UncompressAttachmentHandler.Impl;
using NeosIT.Exchange.GenericExchangeTransportAgent.Tests.Helpers;

namespace NeosIT.Exchange.GenericExchangeTransportAgent.Tests.Plugins
{
    [TestFixture]
    public class UnCompressAttachmentHandlerHandlerTests : OptionsHandlerTestBase<UncompressAttachmentHandler>
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Name = "UncompressAttachmentHandler";
        }

        [Test]
        public void ExtractTest()
        {
            const string list = @"..\..\Samples\152T_01.arj;..\..\Samples\bad.rar;..\..\Samples\testzip.zip;..\..\Samples\testnotpacked.txt";

            var emailMessage = EmailMessageHelper.CreateTextEmailMessage("UncompressAttachmentHandler Subject",
                                                                         "UncompressAttachmentHandler Body");

            foreach (var fileName in list.Split(';'))
            {
                Attachment attachment = emailMessage.Attachments.Add(Path.GetFileName(fileName));
                using (var writeStream = attachment.GetContentWriteStream())
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Open))
                    {
                        fileStream.CopyTo(writeStream);
                    }
                }
            }

            TestObject.Settings[UncompressAttachmentHandler.ExtensionsList] = "zip;arj;rar";

            PrepareLogger();

            TestObject.Execute(new EmailItem(emailMessage));

            Assert.IsTrue(emailMessage.Attachments.Count == 6);

            //test unpacked content
            var testfile = "test file.txt";
            var testfileContent = "Test content";

            var testAttachment = emailMessage.Attachments.FirstOrDefault(a => a.FileName == testfile);
            Assert.IsNotNull(testAttachment, "Test attachment not found.");

            var readStream = testAttachment.GetContentReadStream();

            var sr = new StreamReader(readStream).ReadToEnd();

            Assert.AreEqual(testfileContent, sr);
        }
    }
}
