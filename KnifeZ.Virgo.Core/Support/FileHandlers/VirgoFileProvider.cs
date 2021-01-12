using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;
using KnifeZ.Virgo.Core.Extensions;
using KnifeZ.Virgo.Core.Models;

namespace KnifeZ.Virgo.Core.Support.FileHandlers
{
    public class VirgoFileProvider
    {
        public string SaveMode { get; set; }
        private Dictionary<string, ConstructorInfo> _handlers;
        private ConstructorInfo _defaultHandler;
        private Configs _configs;
        private GlobalData _gd;
        public static Func<IVirgoFileHandler, string> _subDirFunc;

        public VirgoFileProvider(IOptions<Configs> configs, GlobalData gd)
        {
            _configs = configs.Value;
            _gd = gd;
            _handlers = new Dictionary<string, ConstructorInfo>();
            var types = _gd.GetTypesAssignableFrom<IVirgoFileHandler>();
            int count = 1;
            foreach (var item in types)
            {
                var cons = item.GetConstructor(new Type[] { typeof(Configs), typeof(IDataContext) });
                var nameattr = item.GetCustomAttribute<DisplayAttribute>();
                string name = "";
                if (nameattr == null)
                {
                    name = "FileHandler" + count;
                    count++;
                }
                else
                {
                    name = nameattr.Name;
                }
                name = name.ToLower();
                if (name == _configs.FileUploadOptions.SaveFileMode.ToString().ToLower())
                {
                    _defaultHandler = cons;
                }
                _handlers.Add(name, cons);
            }
            if (_defaultHandler == null && types.Count > 0)
            {
                _defaultHandler = types[0].GetConstructor(new Type[] { typeof(Configs), typeof(IDataContext) });
            }
        }

        public VirgoFileProvider(Configs configs)
        {
            _configs = configs;
            _gd = new GlobalData();
            _gd.AllAssembly = Utils.GetAllAssembly();
            _handlers = new Dictionary<string, ConstructorInfo>();
            var types = _gd.GetTypesAssignableFrom<IVirgoFileHandler>();
            int count = 1;
            foreach (var item in types)
            {
                var cons = item.GetConstructor(new Type[] { typeof(Configs), typeof(IDataContext) });
                var nameattr = item.GetCustomAttribute<DisplayAttribute>();
                string name = "";
                if (nameattr == null)
                {
                    name = "FileHandler" + count;
                    count++;
                }
                else
                {
                    name = nameattr.Name;
                }
                name = name.ToLower();
                if (name == _configs.FileUploadOptions.SaveFileMode.ToString().ToLower())
                {
                    _defaultHandler = cons;
                }
                _handlers.Add(name, cons);
            }
            if (_defaultHandler == null && types.Count > 0)
            {
                _defaultHandler = types[0].GetConstructor(new Type[] { typeof(Configs), typeof(IDataContext) });
            }
        }


        public IVirgoFileHandler CreateFileHandler(string saveMode = null, IDataContext dc = null)
        {
            ConstructorInfo ci = null;

            if (string.IsNullOrEmpty(saveMode))
            {
                ci = _defaultHandler;
            }
            else
            {
                saveMode = saveMode.ToLower();
                if (_handlers.ContainsKey(saveMode))
                {
                    ci = _handlers[saveMode];
                }
            }
            if (ci == null)
            {
                return new VirgoDataBaseFileHandler(_configs, dc);
            }
            else
            {
                return ci.Invoke(new object[] { _configs, dc }) as IVirgoFileHandler;
            }
        }

        public IVirgoFile GetFile(string id, bool withData = true, IDataContext dc = null)
        {
            IVirgoFile rv;
            if (dc == null)
            {
                dc = _configs.CreateDC();
            }
            rv = dc.Set<FileAttachment>().CheckID(id).Select(x => new FileAttachment
            {
                ID = x.ID,
                ExtraInfo = x.ExtraInfo,
                FileExt = x.FileExt,
                FileName = x.FileName,
                Length = x.Length,
                Path = x.Path,
                SaveMode = x.SaveMode,
                UploadTime = x.UploadTime
            }).FirstOrDefault();
            if (rv != null && withData == true)
            {
                var fh = CreateFileHandler(rv.SaveMode, dc);
                rv.DataStream = fh.GetFileData(rv);
            }
            return rv;

        }

        public void DeleteFile(string id, IDataContext dc = null)
        {
            FileAttachment file = null;
            if (dc == null)
            {
                dc = _configs.CreateDC();
            }
            file = dc.Set<FileAttachment>().CheckID(id)
                .Select(x => new FileAttachment
                {
                    ID = x.ID,
                    ExtraInfo = x.ExtraInfo,
                    FileExt = x.FileExt,
                    FileName = x.FileName,
                    Path = x.Path,
                    SaveMode = x.SaveMode,
                    Length = x.Length,
                    UploadTime = x.UploadTime
                })
                .FirstOrDefault();
            if (file != null)
            {
                dc.Set<FileAttachment>().Remove(file);
                dc.SaveChanges();
                var fh = CreateFileHandler(file.SaveMode, dc);
                fh.DeleteFile(file);
            }

        }


        public string GetFileName(string id, IDataContext dc = null)
        {
            if (dc == null)
            {
                dc = _configs.CreateDC();
            }
            string rv = dc.Set<FileAttachment>().CheckID(id).Select(x => x.FileName).FirstOrDefault();
            return rv;
        }

        public FileAttachment GetFileModel (string id, IDataContext dc = null)
        {
            if (dc == null)
            {
                dc = _configs.CreateDC();
            }
            FileAttachment rv = dc.Set<FileAttachment>().CheckID(id).FirstOrDefault();
            return rv;
        }

    }
}
