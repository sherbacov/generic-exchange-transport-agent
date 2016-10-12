using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Exchange.Data.Transport.Email;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.Common;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.Common.Impl;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.Common.Impl.Extensions;
using NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.UncompressAttachmentHandler.Impl.Forms;
using SevenZip;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace NeosIT.Exchange.GenericExchangeTransportAgent.Plugins.UncompressAttachmentHandler.Impl
{
    [Export(typeof(IHandler))]
    [DataContract(Name = "UncompressAttachmentHandler", Namespace = "")]
    public class UncompressAttachmentHandler : FilterableHandlerBase, IUncompressAttachmentHandler
    {
        public const string ExtensionsList = "Extensions";
        private IDictionary<string, string> _settings = new Dictionary<string, string> { { ExtensionsList, "" }, };

        [DataMember]
        public IDictionary<string, string> Settings
        {
            get { return _settings; }
            internal set { _settings = value; }
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void ShowConfigDialog()
        {
            var configForm = new ConfigForm(this);
            configForm.ShowDialog(); 
        }

        public override void Execute(IEmailItem emailItem = null, int? lastExitCode = null)
        {
            if (AppliesTo(emailItem, lastExitCode))
            {
                if (null != emailItem &&
                null != emailItem.Message &&
                null != emailItem.Message.Attachments &&
                0 != emailItem.Message.Attachments.Count
                )
                {
                    string extensions = GetSetting(ExtensionsList);

                    var processingList = emailItem.Message.Attachments.ToList();
                    //emailItem.Message.Attachments.Clear();

                    //store all attachments

                    foreach (Attachment attachment in processingList)
                    {
                        Logger.Debug("[GenericTransportAgent] FileName {0}...", attachment.FileName);

                        var ext = Path.GetExtension(attachment.FileName).TrimStart('.');
                        if (ext == null) ext = "";

                        var extList = Settings[ExtensionsList].Split(';');

                        if (extList.Any(l => l == ext))
                        {
                            Logger.Debug("[GenericTransportAgent] FileName contains extension for processing {0}...", ext);

                            var readStream = attachment.GetContentReadStream();
                            readStream.Position = 0;
                            try
                            {
                                using (var szip = new SevenZipExtractor(readStream))
                                {
                                    foreach (var archiveFileInfo in szip.ArchiveFileData)
                                    {
                                        if (archiveFileInfo.IsDirectory) continue;

                                        var attc = emailItem.Message.Attachments.Add(archiveFileInfo.FileName);
                                        var writeStream = attc.GetContentWriteStream();
                                        
                                        szip.ExtractFile(archiveFileInfo.Index, writeStream);

                                        writeStream.Close();

                                        Logger.Debug("[GenericTransportAgent] File added {0} to mail message.", archiveFileInfo.FileName);
                                    }
                                }
                                // Success processing
                                emailItem.Message.Attachments.Remove(attachment);

                            }
                            catch (Exception err)
                            {
                                Logger.Error(err, "[GenericTransportAgent] Error during processing file {0}.", attachment.FileName);
                            }
                        }
                        else
                        {
                            Logger.Debug("[GenericTransportAgent] FileName {0} not contains extension, copy attachment...", attachment.FileName);
                        }
                    }
                }

                if (null != Handlers && Handlers.Count > 0)
                {
                    foreach (IHandler handler in Handlers)
                    {
                        handler.Execute(emailItem, lastExitCode);
                    }
                }
            }
        }

        private static void CopyAttachment(IEmailItem emailItem, Attachment attachment)
        {
            var attc = emailItem.Message.Attachments.Add(attachment.FileName, attachment.ContentType);

            var readStream = attachment.GetContentReadStream();
            readStream.Position = 0;

            var writeStream = attc.GetContentWriteStream();
            readStream.CopyTo(writeStream);
        }

        public override string Name
        {
            get { return "UncompressAttachmentHandler"; }
        }

        private string GetSetting(string key)
        {
            return _settings.ContainsKey(key) ? _settings[key] : string.Empty;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
