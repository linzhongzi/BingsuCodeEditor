using BingsuCodeEditor.AutoCompleteToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingsuCodeEditor
{
    public abstract class ImportManager
    {
        /// <summary>
        /// 检索文件内容的选项函数
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public abstract string GetFIleContent(string pullpath);


        /// <summary>
        /// 打开文件并转到相应行函数。
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public abstract void OpenFile(string pullpath, int offset);


        /// <summary>
        /// 如何获取基本文件函数
        /// </summary>
        /// <returns></returns>
        public abstract List<string> GetFIleList();


        public CodeTextEditor.CodeType CodeType;

        /// <summary>
        ///检查文件是否被缓存
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool IsCachedContainer(string pullpath)
        {
            return CachedContainer.Keys.Contains(pullpath);
        }


        /// <summary>
        ///检查文件是否被缓存
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public void CachedContainerRemove(string pullpath)
        {
            if (CachedContainer.Keys.Contains(pullpath))
            {
                //如果存在
                CachedContainer.Remove(pullpath);
            }
        }



        //如果文件未被修改，请从此处获取。
        private Dictionary<string, Container> CachedContainer = new Dictionary<string, Container>();

        /// <summary>
        /// 有机会取回容器函数
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public void UpdateContainer(string pullpath, Container container)
        {
            container.pullpath = pullpath;

            if (CachedContainer.Keys.Contains(pullpath))
            {
                CachedContainer[pullpath] = container;
            }
            else
            {
                CachedContainer.Add(pullpath, container);
            }
        }


        /// <summary>
        /// 有机会取回容器函数
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Container GetContainer(string pullpath)
        {
            return CachedContainer[pullpath];
        }

     



        /// <summary>
        /// 如何检查文件是否存在函数
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool IsFileExist(string filename, string basefilename = "")
        {
            return GetImportedFileList(basefilename).IndexOf(filename) != -1;
        }

        /// <summary>
        /// 获取所有当前存在的文件。
        /// </summary>
        /// <param name="filename">当前文件的名称</param>
        /// <returns></returns>
        public abstract List<string> GetImportedFileList(string basefilename = "");


        /// <summary>
        /// 获取文件的绝对地址。
        /// </summary>
        /// <param name="filename">当前文件的名称。</param> 
		/// <param name="basefilename">包含该文件的文件夹</param>
        /// <returns></returns>
        public string GetPullPath(string filename, string basefilename = "")
        {
            char spliter = '\'';
            string redo = "..";


            List<string> flist = GetImportedFileList();


            // 连接时，如果输入全名 filenamed | ..，则必须将其从基本文件名中删除。
            List<string> btlist = basefilename.Split('.').ToList();
            string f = filename.Replace(redo, "/" + spliter);
            List<string> ftlist = f.Split(spliter).ToList();

            while(ftlist.Count > 0)
            {
                string fstr = ftlist.First();
                ftlist.RemoveAt(0);

                //如果是后退字符，则将其从 BT 中删除。
                if (fstr == "/")
                {
                    if(btlist.Count == 0)
                    {
                        return "";
                    }
                    btlist.RemoveAt(btlist.Count - 1);
                }
                else
                {
                    //如果没有，请添加
                    btlist.Add(fstr);
                }
            }

            string connectname = "";

            for (int j = 0; j < btlist.Count; j++)
            {
                if(j != 0) connectname += ".";
                connectname += btlist[j];
            }

            int i = flist.IndexOf(connectname);
            if(i != -1) return flist[i];


            //全名本身
            i = flist.IndexOf(filename);
            if (i != -1) return flist[i];

            return "";
        }





        public abstract string GetDefaultFunctions();
    }
}
