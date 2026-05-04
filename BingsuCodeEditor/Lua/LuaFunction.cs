using BingsuCodeEditor.AutoCompleteToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingsuCodeEditor.Lua
{
    internal class LuaFunction : Function
    {
        public LuaFunction(Container parentcontainer, CodeAnalyzer.TOKEN StartToken) : base(parentcontainer, StartToken)
        {
        }

        private void SetSummary(string type, string sectype, string content, string argtype)
        {
            switch (type)
            {
                case "@Type":
                    special = content;
                    if (preCompletion != null)
                    {
                        switch (content)
                        {
                            case "A":
                                preCompletion.completionWordType = CodeAnalyzer.CompletionWordType.Action;
                                break;
                            case "C":
                                preCompletion.completionWordType = CodeAnalyzer.CompletionWordType.Condiction;
                                break;
                        }
                    }
                    break;
                case "@Summary":
                    funcsummary = content;
                    break;
                case "@param":
                    if (!argsummary.ContainsKey(sectype)) {
                        argsummary.Add(sectype, content);
                    }
                    Arg arg = args.Find((x) => x.argname == sectype);

                    if(arg != null)
                    {
                        arg.argtype = argtype;
                    }

                    break;
            }


        }

        public override void ReadComment(string launage)
        {
            argsummary.Clear();
            /***
--[================================[
@Language.ko-KR
@Summary
[Player]의 [Unit]의 유닛보유수를 [Amount]만큼 [Modifier]합니다.
@Group
유닛보유수
@param.Unit.TrgUnit
@param.Player.TrgPlayer
@param.Modifier.TrgModifier
@param.Amount.Number


@Language.en-US
@Summary
[Player]'s [Unit] unit count is [Modifier] by [Amount].
@Group
Number of Units Owned
@param.Unit.TrgUnit
@param.Player.TrgPlayer
@param.Modifier.TrgModifier
@param.Amount.Number


@Language.zh-CN
@Summary
将[Player]的[Unit]数量[Modifier]为[Amount]。
@Group
持有单位数量
@param.Unit.TrgUnit
@param.Player.TrgPlayer
@param.Modifier.TrgModifier
@param.Amount.Number
]================================]
            ***/
            if (string.IsNullOrEmpty(comment)) return;

            string c = this.comment.Trim();

            string[] line = c.Split('\n');

            if(c.IndexOf("[Player]의 [Unit]의 유닛보유수를 [Amount]만큼 [Modifier]합니다.") != -1)
            {

            }


            string summaryType = "";
            string argType = "";
            string argName = "";
            string clan = "";
            string content = "";

            for (int i = 0; i < line.Count(); i++)
            {
                if (string.IsNullOrEmpty(line[i])) continue;
                string[] o = line[i].Split('.');

                string currentType = o[0].Trim();

                switch (currentType)
                {
                    case "@Language":
                        if (summaryType != "")
                        {
                            //如果选中，则组织上次读取的 ctype。
                            if (clan == launage)
                            {
                                SetSummary(summaryType, argName, content.Trim(), argType);
                            }
                        }
                        clan = o.Last().Trim();
                        break;
                    case "@Group":
                    case "@Summary":
                    case "@param":
                        //如果是summaryType.
                        if (summaryType != "")
                        {
                            //如果选中，则组织上次读取的 ctype。
                            if (clan == launage)
                            {
                                SetSummary(summaryType, argName, content.Trim(), argType);
                            }
                        }
                        summaryType = currentType;
                        if (currentType == "@param")
                        {
                            argName = o[1].Trim();
                            argType = o[2].Trim();
                        }

                        content = "";
                        break;
                    default:
                        content += line[i].Trim();
                        break;
                }
            }

            //组织最后读取的 ctype。
            if (clan == launage)
            {
                SetSummary(summaryType, argName, content.Trim(), argType);
            }


        }
    }
}
