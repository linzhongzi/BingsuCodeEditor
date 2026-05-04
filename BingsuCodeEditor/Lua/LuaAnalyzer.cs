using BingsuCodeEditor.AutoCompleteToken;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static BingsuCodeEditor.CodeAnalyzer;

namespace BingsuCodeEditor.Lua
{
    class LuaAnalyzer : CodeAnalyzer
    {
        public ImportManager importManager;
        public string DEFAULTFUNCFILENAME = "DEFAULTFUNCTIONLIST";
        public Container _DefaultFuncContainer;
        public Container DefaultFuncContainer
        {
            get
            {
                return _DefaultFuncContainer;
            }
        }

        public override ImportManager StaticImportManager
        {
            get
            {
                return importManager;
            }
        }

        public override Container GetDefaultContainer
        {
            get
            {
                return _DefaultFuncContainer;
            }
        }

        public override void SetImportManager(ImportManager importManager)
        {
            this.importManager = importManager;

            if (_DefaultFuncContainer == null)
            {
                _DefaultFuncContainer = new Container(this);

                {
                    Container c = this.GetContainer(importManager.GetFIleContent(DEFAULTFUNCFILENAME));
                    _DefaultFuncContainer.funcs.AddRange(c.funcs);

                }

                foreach (var item in importManager.GetFIleList())
                {
                    if(item == "msqcTool.lua")
                    {

                    }
                    Container c = GetContainer(importManager.GetFIleContent(item));

                    _DefaultFuncContainer.funcs.AddRange(c.funcs);
                }

                foreach (var item in _DefaultFuncContainer.funcs)
                {
                    if (!string.IsNullOrEmpty(item.comment)) item.ReadComment("ko-KR");
                    //if (!string.IsNullOrEmpty(item.comment)) item.ReadComment("zh-CN");
                }
            }

            if (!importManager.IsCachedContainer(DEFAULTFUNCFILENAME))
            {
                importManager.UpdateContainer(DEFAULTFUNCFILENAME, _DefaultFuncContainer);
            }

        }



        public LuaAnalyzer(TextEditor textEditor) : base(textEditor, false)
        {
            string[] keywords = {"and", "break", "do", "else", "elseif",
                "end", "false", "for", "function", "if",
                "in", "local", "nil", "not", "or",
                "repeat", "return", "then", "true", "until",
                "while"};


            //[tab]for(var [i] = 0; [i] < [Length] ; [i]++)\n[tab]{\n[tab][tabonce][Content]\n[tab]}
            Template.Add("if", " [true] then\n[tab][tabonce][Content]\n[tab]end");
            Template.Add("while", " [true] do\n[tab][tabonce][Content]\n[tab]end");
            Template.Add("repeat", "\n[tab][tabonce][Content]\n[tab]until [true]");
            Template.Add("for", " [index] = [1], [Length] do\n[tab][tabonce][Content]\n[tab]end");
            Template.Add("function", " [FuncName]([Arg]) \n[tab][tabonce][Content]\n[tab]end");
            // //Template.Add("/***", "\n[tab] * @Type\n[tab] * F\n[tab] * @Summary.ko-KR\n[tab] * [Summary]\n[tab] * @param.args.ko-KR\n[tab]***/[Content]");
            //Template.Add("/***", "\n[tab] * @Type\n[tab] * F\n[tab] * @Summary.zh-CN\n[tab] * [Summary]\n[tab] * @param.args.zh-CN\n[tab]***/[Content]");


            DEFAULTFUNCFILENAME = "DEFAULTFUNCTIONLIST";


            foreach (var item in keywords)
            {
                //输入Token
                AddSubType(item, TOKEN_TYPE.KeyWord);

                //自动完成初始输入
                if (Template.ContainsKey(item))
                {
                    completionDatas.Add(new KewWordItem(CompletionWordType.KeyWord, item, item + "\n注意：要插入代码片段，请按 Tab 键两次。"));
                }
                else
                {
                    completionDatas.Add(new KewWordItem(CompletionWordType.KeyWord, item));
                }
            }


            tokenAnalyzer = new LuaTokenAnalyzer(this);
            secondtokenAnalyzer = new LuaTokenAnalyzer(this);
            //codeFoldingManager = new LuaFoldingManager(textEditor);
        }
        public override bool AutoInsert(string text)
        {
            return false;
        }

        public override bool AutoRemove()
        {
            return false;
        }
        public override TOKEN TokenBlockAnalyzer(string text, int index, out int outindex, int caretoffset)
        {
            int sindex = index;
            char t = text[index];
            int tlen = text.Length;
            string block = t.ToString();

            outindex = -1;

            if (t == '"')
            {
                block = "";
                //LineCommnet 重复直到换行 (\r)字符

                bool IsSpec = false;
                do
                {
                    block += t.ToString();


                    index++;
                    if (index >= tlen)
                    {
                        break;
                    }
                    t = text[index];


                    if (t == '"' && !IsSpec)
                    {
                        break;
                    }

                    if (t == '\\' && IsSpec == false)
                    {
                        IsSpec = true;
                    }
                    else
                    {
                        IsSpec = false;
                    }


                } while (index < tlen);
                block += '"';

                TOKEN_TYPE type = TOKEN_TYPE.String;

                TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                outindex = index;
                return token;
            }
            else if (t == '-')
            {
                if (index + 1 >= tlen)
                {
                    return null;
                }
                char nt = text[index + 1];

                if (nt == '-')
                {


                    index++;
                    if (index + 1 >= tlen)
                    {
                        return null;
                    }

                    t = text[index + 1];
                    if(t == '[')
                    {
                        int equalcount = 0;
                        //MulitComment =====[ 重复直到出现计数


                        index += 2;
                        if (index >= tlen)
                        {
                            return null;
                        }
                        t = text[index];
                        while (t != '[' && index < tlen)
                        {
                            t = text[index++];

                            if (t == '=')
                            {
                                equalcount++;
                            }
                        }
                        // t ='='

                        int checkequalcount = -1;

                        index++;
                        if (index >= tlen)
                        {
                            return null;
                        }
                        block = "";
                        do
                        {
                            index++;
                            if (index >= tlen)
                            {
                                //其实是一个错误...
                                break;
                            }
                            if (t == ']')
                            {
                                if(checkequalcount == -1)
                                {
                                    checkequalcount = 0;
                                }else if(checkequalcount == equalcount)
                                {
                                    block = block.Substring(0, block.Length - equalcount - 2);
                                    //注释
                                    break;
                                }
                                else
                                {
                                    checkequalcount = -1;
                                }

                            }

                            if(t == '=' && checkequalcount >= 0)
                            {
                                checkequalcount++;
                            }

                            t = text[index];
                            block += t.ToString();
                        } while (index < tlen);
                        index--;


                        TOKEN_TYPE type = TOKEN_TYPE.Comment;

                        //block = block.Replace("\r", "");

                        TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                        outindex = index;
                        return token;
                    }
                    else
                    {
                        //LineCommnet 重复直到换行 (\r)字符
                        do
                        {
                            block += t.ToString();
                            index++;
                            if (index >= tlen)
                            {
                                break;
                            }
                            t = text[index];
                        } while (index < tlen && (t != '\n'));
                        index--;


                        TOKEN_TYPE type = TOKEN_TYPE.LineComment;

                        block = block.Replace("\r", "");

                        TOKEN token = new TOKEN(sindex, type, block, caretoffset);
                        outindex = index;
                        return token;
                    }
                    
                }
            }
         

            return null;
        }

        public override bool GetCompletionList(IList<ICompletionData> data, bool IsNameSpaceOpen = false)
        {
            string scope = maincontainer.currentScope;



            Container container = maincontainer;
            Container objcontainer = null;


            if (scope.IndexOf("st.O") != -1)
            {
                //string[] scopes = scope.Split('.');
                //string initscope = scopes[0] + "." + scopes[1];

                List<string> tname = new List<string>();
                tname.Add(scope.Split('.')[1].Substring(1));
                object _obj = GetObjectFromName(tname, maincontainer, FindType.Obj, scope: "st");

                if (_obj != null)
                {
                    objcontainer = (Container)_obj;
                }
            }





            switch (cursorLocation)
            {
                case CursorLocation.ImportFile:
                    //显示文件
                    if (importManager != null)
                    {
                        if (!IsNameSpaceOpen)
                        {
                            string fname = "";

                            TOKEN ctkn = GetToken(0, TOKEN.Side.Right);
                            List<TOKEN> t = tokenAnalyzer.GetTokenListFromTarget(ctkn, true);

                            List<string> strs = new List<string>();
                            for (int i = 0; i < t.Count - 1; i++)
                            {
                                strs.Add(t[i].Value);

                                fname += t[i].Value;
                                fname += ".";
                            }


                            if (fname == "")
                            {
                                foreach (var item in importManager.GetImportedFileList())
                                {
                                    data.Add(new CodeCompletionData(new ImportFileItem(CompletionWordType.nameSpace, item)));
                                }
                                return true;
                            }
                            else
                            {
                                foreach (var item in importManager.GetImportedFileList())
                                {
                                    string tstr = item;
                                    if (tstr.StartsWith(fname))
                                    {
                                        tstr = tstr.Replace(fname, "");
                                        data.Add(new CodeCompletionData(new ImportFileItem(CompletionWordType.nameSpace, tstr)));
                                    }
                                }
                                return true;
                            }
                        }
                    }
                    break;
            }


            //TODO：使用分析的标记创建自动完成功能。
            if (IsNameSpaceOpen)
            {
                TOKEN ctkn = GetToken(0, TOKEN.Side.Right);
                if (ctkn == null) return true;

                List<TOKEN> t = tokenAnalyzer.GetTokenListFromTarget(ctkn, true);
                //imported1.var1;
                //imported1.const1.object1;
                //const1.object1;
                //maincontainer.vars[0].

                //Item.cast(inven[i]).
                //在本例中，Item.cast 被发送到 t。
                //我们需要找到 Item.cast函数的返回类型。
                //t.Add(new TOKEN(0, TOKEN_TYPE.Identifier, "Item", 0));
                //t.Add(new TOKEN(0, TOKEN_TYPE.Identifier, "cast", 0));
                if (t.Count == 0) return true;


                List<string> strs = new List<string>();

                string fname = "";
                foreach (var item in t)
                {
                    strs.Add(item.Value);

                    fname += item.Value;
                    fname += ".";
                }

                string last = GetDirectText(0);
                if (cursorLocation == CursorLocation.ImportFile && importManager != null)
                {
                    bool IsImport = false;
                    List<string> filelist = importManager.GetImportedFileList(maincontainer.folderpath);
                    List<string> autocmpfilelist = new List<string>();
                    foreach (var item in filelist)
                    {
                        if (item.StartsWith(fname))
                        {
                            //如果它们匹配
                            IsImport = true;
                            autocmpfilelist.Add(item.Substring(fname.Length));
                        }
                    }
                    if (IsImport)
                    {
                        foreach (var item in autocmpfilelist)
                        {
                            data.Add(new CodeCompletionData(new ImportFileItem(CompletionWordType.nameSpace, item)));
                        }
                    }
                }



                GetObjectFromName(strs, container, FindType.AutoComplete, data, scope);


                //foreach (var item in t)
                //{
                //    PreCompletionData preCompletionData = new ImportFileItem(CompletionWordType.nameSpace, item.Value);
                //    data.Add(new CodeCompletionData(preCompletionData));
                //}

                return true;
            }
            switch (cursorLocation)
            {
                case CursorLocation.FunctionArgType:
                case CursorLocation.VarTypeDefine:
                    if (cursorLocation == CursorLocation.FunctionArgType)
                    {
                        foreach (var item in LuaDefaultCompletionData.GetCompletionKeyWordList())
                        {
                            data.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in container.objs)
                        {
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Variable, item.mainname)));
                        }
                        foreach (var item in DefaultFuncContainer.objs)
                        {
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Variable, item.mainname)));
                        }
                    }

                    return true;
            }

            if (maincontainer.innerFuncInfor.IsInnerFuncinfor)
            {
                //参数
                Function func = (Function)GetObjectFromName(maincontainer.innerFuncInfor.funcename, maincontainer, FindType.Func);
                if (func != null)
                {
                    if (func.args.Count <= maincontainer.innerFuncInfor.argindex)
                    {
                        return true;
                    }
                    string argtype = func.args[maincontainer.innerFuncInfor.argindex].argtype;

                    foreach (var item in LuaDefaultCompletionData.GetCompletionDataList(argtype))
                    {
                        data.Add(item);
                    }
                }
            }
            else
            {
                if (base.GetCompletionList(data)) return true;
            }




            container.GetAllItems(data, scope);
            if (objcontainer != null)
            {
                data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Const, "this")));
                objcontainer.GetAllItems(data, scope);
            }
            if (DefaultFuncContainer != null) { DefaultFuncContainer.GetAllItems(data, "st"); }
            if (importManager != null)
            {
                foreach (var pullpath in importManager.GetImportedFileList())
                {
                    if(pullpath != FilePath)
                    {
                        if (importManager.IsFileExist(pullpath))
                        {
                            if (!importManager.IsCachedContainer(pullpath))
                            {
                                //如果文件已被修改
                                importManager.UpdateContainer(pullpath, GetContainer(importManager.GetFIleContent(pullpath)));
                            }
                        }
                        importManager.GetContainer(pullpath).GetAllItems(data, "st");
                    }
                }
            }

            


            return true;
        }

        public override void TokenAnalyze(int caretoffset = int.MaxValue)
        {
            CursorLocation cl = CursorLocation.None;


            //TOKEN clefttoken = GetToken(0, TOKEN.Side.Left);

            TOKEN ctoken = GetToken(0);
            TOKEN btoken = GetToken(-1);
            TOKEN bbtoken = GetToken(-2);

            if (ctoken != null)
            {
                if (ctoken.Type == TOKEN_TYPE.Symbol && ctoken.Value == ";")
                {
                    ctoken = GetToken(-1);
                    btoken = GetToken(-2);
                    bbtoken = GetToken(-3);
                }
                if (ctoken.Type == TOKEN_TYPE.KeyWord && ctoken.Value == "function")
                {
                    cl = CursorLocation.FunctionName;
                }
            }

            if (btoken != null)
            {
                if (btoken.Type == TOKEN_TYPE.KeyWord)
                {
                    switch (btoken.Value)
                    {
                        case "function":
                            cl = CursorLocation.FunctionName;
                            break;
                    }
                }
            }


            if (cl == CursorLocation.None)
            {
                if (ctoken != null && ctoken.Type == TOKEN_TYPE.Symbol && ctoken.Value == ":")
                {
                    cl = CursorLocation.VarTypeDefine;
                }
                if (btoken != null && btoken.Type == TOKEN_TYPE.Symbol && btoken.Value == ":")
                {
                    cl = CursorLocation.VarTypeDefine;
                }
                //if(ctoken != null)
                //{
                //    int argpos;
                //    tokenAnalyzer.GetWritedFunction(ctoken, out argpos);
                //}
            }


            cursorLocation = cl;


            //词元(Token)分析中使用的元素
            tokenAnalyzer.Init(Tokens);

            try
            {
                maincontainer = tokenAnalyzer.ConatainerAnalyzer(caretoffset);
                //RefershImportContainer(maincontainer);

                if (maincontainer.cursorLocation != CursorLocation.None)
                {
                    cursorLocation = maincontainer.cursorLocation;
                }
            }
            catch (Exception e)
            {
                TOKEN errortoken = tokenAnalyzer.GetLastToken;
                if (!(errortoken is null))
                {
                    tokenAnalyzer.ThrowException(e.ToString(), tokenAnalyzer.GetLastToken);
                }
                //tokenAnalyzer.ErrorMessage;

                //return;
            }
            if (tokenAnalyzer.IsError)
            {
                //词元(Token)解析错误
            }


            tokenAnalyzer.Complete(textEditor);
        }

        public override object GetObjectFromName(List<TOKEN> tokenlist, Container startcontainer, FindType findType, IList<ICompletionData> data = null, string scope = "st")
        {
            if (tokenlist == null) return null;

            List<string> strs = new List<string>();

            foreach (var item in tokenlist)
            {
                string rval;

                rval = item.Value;

                strs.Add(rval);
            }

            return GetObjectFromName(strs, startcontainer, findType, data, scope);
        }

        public override object GetObjectFromName(List<string> objectname, Container startcontainer, FindType findType, IList<ICompletionData> data = null, string scope = "st")
        {
            //imported1.var1;
            //imported1.const1.object1;
            //const1.object1;
            //maincontainer.vars[0].

            //寻找参考文献，如果找不到，请忽略。
            //if (!startcontainer.CheckIdentifier(scope, objectname[0]))
            //{
            //    return null;
            //}
            if (objectname.Count == 0) return null;

            Container ccon = startcontainer;
            Container objcon = null;
            string folderpath = folder;
            int index = 0;
            string lscope = scope;
            while (true)
            {
                string objname = objectname[index];


                Block var = null;
                Container obj = null;
                Function func = null;
                ImportedNameSpace importedNameSpace = null;



                if (objcon != null)
                {
                    var = objcon.vars.Find(x => (x.BlockName == objname && lscope.Contains(x.Scope)));
                    obj = objcon.objs.Find(x => (x.mainname == objname));
                    func = objcon.funcs.Find(x => (x.funcname == objname && lscope.Contains(x.scope)));
                }
                else
                {
                    var = ccon.vars.Find(x => (x.BlockName == objname && lscope.Contains(x.Scope)));
                    obj = ccon.objs.Find(x => (x.mainname == objname));
                    func = ccon.funcs.Find(x => (x.funcname == objname && lscope.Contains(x.scope)));
                    importedNameSpace = ccon.importedNameSpaces.Find(x => (x.shortname == objname));
                }

                if (DefaultFuncContainer != null)
                {
                    if (var == null)
                    {
                        var = DefaultFuncContainer.vars.Find(x => (x.BlockName == objname && lscope.Contains(x.Scope)));
                    }
                    if (obj == null)
                    {
                        obj = DefaultFuncContainer.objs.Find(x => (x.mainname == objname));
                    }
                    if (func == null)
                    {
                        func = DefaultFuncContainer.funcs.Find(x => (x.funcname == objname && lscope.Contains(x.scope)));
                    }
                }

                //if (objname == "this" && objectname.Count == 1)
                //{
                // //如果是自引用
                //    if (findType == FindType.AutoComplete)
                //    {
                // //这是最后一部分，所以输入容器的所有内容。
                //        ccon.GetAllItems(data, "st.O" + startcontainer.mainname);
                //    }
                //}


                if (importedNameSpace != null)
                {
                    string nsfile = importedNameSpace.mainname;
                    string pullpath = importManager.GetPullPath(nsfile, folderpath);

                    if (importManager.IsFileExist(pullpath))
                    {
                        if (!importManager.IsCachedContainer(pullpath))
                        {
                            //如果文件已被修改
                            importManager.UpdateContainer(pullpath, GetContainer(importManager.GetFIleContent(pullpath)));
                        }

                        ccon = importManager.GetContainer(pullpath);

                        if (pullpath.IndexOf(".") != -1)
                        {
                            folderpath = pullpath.Substring(0, pullpath.LastIndexOf("."));
                        }
                        else
                        {
                            folderpath = "";
                        }

                        lscope = "st";//初始化范围
                        index++;
                        if (index == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                ccon.GetAllItems(data, lscope);
                            }
                            else if (findType == FindType.All)
                            {
                                return importedNameSpace;
                            }

                            break;
                        }
                    }
                    else
                    {
                        return null;
                    }

                }
                else if (func != null)
                {
                    if (findType == FindType.Func || findType == FindType.All)
                    {
                        //如果最后有差异
                        if (index + 1 == objectname.Count)
                        {
                            //如果您正在寻找解决方案，只需返回此函数即可。哈哈。
                            return func;
                        }

                        break;
                    }
                    else if (findType == FindType.AutoComplete)
                    {
                        //如果是自动完成，则注入相应数字的返回类型函数。
                        string rtype = "";

                        if (func.returntype == null) return null;

                        foreach (var item in func.returntype)
                        {
                            if (rtype != "") rtype += ",";
                            rtype += item.Value;
                        }


                        if (rtype == null) return null;

                        List<string> tname = new List<string>();
                        tname.AddRange(rtype.Split('.'));
                        object _obj = GetObjectFromName(tname, ccon, FindType.Obj);

                        if (_obj != null)
                        {
                            objcon = (Container)_obj;
                        }
                        else
                        {
                            return null;
                        }


                        if (index + 1 == objectname.Count)
                        {
                            //如果是最后一个订单，则返回对象的元素。
                            objcon.GetAllItems(data, objcon.GetInitObjectNameSpacee(), noargFlag: true);
                            break;
                        }

                    }


                    return null;
                }
                else if (obj != null)
                {
                    if (findType == FindType.Obj)
                    {
                        if (index + 2 == objectname.Count)
                        {
                            switch (objectname[index + 1])
                            {
                                case "cast":
                                case "alloc":
                                    return obj;
                            }
                        }
                    }




                    if (index + 1 == objectname.Count)
                    {
                        if (findType == FindType.AutoComplete)
                        {
                            //由于这是最后一部分，因此请输入容器的所有内容。
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "cast")));
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "alloc")));
                            data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "free")));
                        }
                        else if (findType == FindType.Obj || findType == FindType.All)
                        {
                            return obj;
                        }


                        break;
                    }
                    else
                    {

                        if (index + 1 < objectname.Count)
                        {
                            string last = objectname[index + 1];

                            if (last == "alloc" || last == "cast")
                            {
                                if (index + 2 == objectname.Count)
                                {
                                    //如果最后一个是 alloc 或cast
                                    if (findType == FindType.AutoComplete)
                                    {
                                        obj.GetAllItems(data, obj.GetInitObjectNameSpacee(), noargFlag: true);
                                        break;
                                    }
                                    else if (findType == FindType.Func)
                                    {
                                        if (last == "alloc")
                                        {
                                            //构造函数导入
                                            func = obj.funcs.Find(x => (x.funcname == "constructor" && obj.GetInitObjectNameSpacee().Contains(x.scope)));
                                            return func;
                                        }
                                    }
                                    return null;
                                }
                                else
                                {
                                    //引用一个对象。
                                    lscope = obj.GetInitObjectNameSpacee();
                                    objcon = obj;
                                    index += 2;
                                }
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else if (var != null)
                {
                    //参考变量的定义检查类型。
                    //如果是 obj，则 obj 的成员 ex var t = a.b.c();如果是这种情况，您需要搜索 a.b.c变量。
                    //var.values
                    //就 obj 而言，由于 obj 是一个容器...您可以在查找数字的过程中更进一步函数。
                    object _obj;


                    if (!string.IsNullOrEmpty(var.BlockType))
                    {
                        List<string> list = new List<string>();

                        list.Add(var.BlockType);


                        //如果指定类型
                        if (ccon.IsObject)
                        {
                            //如果是物体，就得到外面去寻找。

                        }
                        _obj = GetObjectFromName(list, ccon, FindType.Obj);

                        if (_obj == null && DefaultFuncContainer != null)
                        {
                            _obj = GetObjectFromName(list, DefaultFuncContainer, FindType.Obj);
                        }


                        if (objcon != null && var.BlockType == "selftype")
                        {
                            _obj = objcon;
                        }
                    }
                    else
                    {
                        //其他的
                        _obj = GetObjectFromName(var.Values, ccon, FindType.Obj);
                        if (_obj == null && DefaultFuncContainer != null)
                        {
                            _obj = GetObjectFromName(var.Values, DefaultFuncContainer, FindType.Obj);
                        }
                        if (_obj == null)
                        {
                            //函数可能存在
                            _obj = GetObjectFromName(var.Values, ccon, FindType.Func);
                            if (_obj == null && DefaultFuncContainer != null)
                            {
                                _obj = GetObjectFromName(var.Values, DefaultFuncContainer, FindType.Func);
                            }
                            if (_obj != null)
                            {
                                Function funcobject = (Function)_obj;
                                List<string> list = new List<string>();

                                foreach (var item in funcobject.returntype)
                                {
                                    list.Add(item.Value);
                                }
                                //读取返回值

                                _obj = GetObjectFromName(list, ccon, FindType.Obj);
                            }
                        }
                    }


                    if (_obj != null)
                    {
                        Container varobject = (Container)_obj;
                        ccon = varobject;
                        lscope = "st";//初始化范围
                        index++;
                        if (index == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                ccon.GetAllItems(data, ccon.GetInitObjectNameSpacee());
                            }
                            else if (findType == FindType.All)
                            {
                                return _obj;
                            }


                            break;
                        }
                    }
                    else
                    {
                        //或者使用下面的一个
                        if (index + 1 == objectname.Count)
                        {
                            if (findType == FindType.AutoComplete)
                            {
                                //由于这是最后一部分，因此请输入容器的所有内容。
                                //data.Add(new CodeCompletionData(new ObjectItem(CompletionWordType.Function, "getValueAddr")));
                            }
                            else if (findType == FindType.All)
                            {
                                return var;
                            }

                            break;
                        }
                        else
                        {
                            return null;
                        }
                    }



                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public override string GetTooiTipText(TOKEN token)
        {
            switch (token.Type)
            {
                case TOKEN_TYPE.Identifier:
                    break;
                default:
                    return "";
            }


            List<TOKEN> t = tokenAnalyzer.GetTokenListFromTarget(token, true);

            string rstr = "";
            foreach (var item in t)
            {
                if (rstr != "")
                {
                    rstr += ".";
                }

                rstr += item.Value;
            }

            object o = GetObjectFromName(t, maincontainer, FindType.All, null, token.scope);

            if (o != null)
            {
                switch (o.GetType().Name)
                {
                    case "Block":
                        Block var = (Block)o;
                        rstr = var.BlockDefine + " " + rstr;
                        if (!string.IsNullOrEmpty(var.BlockType))
                        {
                            rstr += ":" + var.BlockType;
                        }

                        if (var.Values != null && var.Values.Count != 0)
                        {
                            string v = "";
                            foreach (var item in var.Values)
                            {
                                if (v != "")
                                {
                                    v += ".";
                                }

                                v += item.Value;
                            }

                            rstr += " = " + v;
                        }
                        if (var.IsArg)
                        {
                            rstr = "(范围) " + rstr;
                        }

                        break;
                    case "Container":
                        Container obj = (Container)o;
                        if (rstr == obj.mainname)
                        {
                            rstr = "object " + rstr;
                        }
                        else
                        {
                            rstr = rstr + ":" + obj.mainname;
                        }
                        break;
                    case "EpScriptFunction":
                        Function func = (Function)o;
                        rstr = GetFuncToolTip(t).Trim();
                        break;
                    case "ImportedNameSpace":
                        ImportedNameSpace importedNameSpace = (ImportedNameSpace)o;
                        rstr = "import " + importedNameSpace.mainname + " as " + importedNameSpace.shortname;
                        break;
                }
            }

            //Block var = null;
            //Container obj = null;
            //Function func = null;
            //ImportedNameSpace importedNameSpace = null;
            return rstr;
        }

        public override void SetCommentLine(int start, int end, string intend, CommentType commentType)
        {
            throw new NotImplementedException();
        }
    }
}
