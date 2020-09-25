
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DTOCreator
{
   public class UE_CPP
    {
        static string classStr = @"
#include ""SerializeStruct.h""
#include ""MiniEncode.h""

/*
    the functions copy to blueprint function library !!!
    UFUNCTION(BlueprintCallable)
	static TArray<ClassName> ToClassNameArray(TArray<uint8> data)
    {
        TArray<ClassName> value;
		MiniEncode mini(data);
		mini.d(value);
		return value;
    }

    UFUNCTION(BlueprintCallable)
    static TArray<uint8> ClassNameArrayTo(TArray<ClassName> data)
    {
        MiniEncode mini;
		mini.e(data);
		return mini.GetData();
    }
*/
USTRUCT(BlueprintType)
struct ClassName : public FSerializeBase
{
    GENERATED_USTRUCT_BODY()
##vars
##Constructor
    TArray<uint8> encode() {
		ByteArray arr;
		encode(arr);
		return arr.Buffer();
	}
	bool decode(TArray<uint8> data) {
		ByteArray arr(data);
		return decode(arr);
	}
    
    bool decode(ByteArray& data) override 
    {
##debuild
        return true;
    }

    void encode(ByteArray& data) override 
    {
##build
    }
};";
 
        public static void Build(string calssName,string varStr,bool Constructor,string gopackage)
        {
            string result = classStr.Replace("ClassName", "F"+calssName);
            string[] vars = varStr.Split('\n');
            string info = "\n\t";
            List<VarInfo> infos = new List<VarInfo>();
            for (int i = 0; i < vars.Length; i++)
            {
                string line = vars[i].Trim().Replace("\r", "");
                LineProcess(line, ref info, ref infos);
            }
            string varString = "";
            for (int i = 0; i < infos.Count; i++)
            {
                varString += infos[i].about + "UPROPERTY(EditAnywhere, BlueprintReadWrite)\n\t" + infos[i].type + " " + infos[i].value + "\n";
            }
            result = result.Replace("##vars", varString);
            result = result.Replace("##build", ProcessEncode(infos));
            result = result.Replace("##debuild", ProcessDecode(infos));
            //因gc原因 改用struct结构体 不需要构造 废弃
            //if (checkBox1.Checked)
            //{
            //    string constructor = "\t";
            //    constructor += "public " + ClassNameBox.Text.Trim() + "(){}\n\t";
            //    constructor += "public " + ClassNameBox.Text.Trim() + "(";
            //    for (int i = 0; i < infos.Count; i++)
            //    {
            //        constructor += infos[i].type + " " + infos[i].value.Replace(";", "");
            //        if (i != infos.Count - 1) constructor += ",";
            //    }
            //    constructor += ")\n\t{";
            //    for (int i = 0; i < infos.Count; i++)
            //    {
            //        constructor += "\n\t\tthis." + infos[i].value.Replace(";", "") + "=" + infos[i].value.Replace(";", "") + ";";
            //    }
            //    constructor += "\n\t}";
            //    result = result.Replace("##Constructor", constructor);
            //}
            //else
            {
                result = result.Replace("##Constructor", "");
            }
            result += "\r\n";
            File.WriteAllText(calssName + ".h", result);
            MessageBox.Show("导出" + calssName + ".h成功");
        }

        static string ProcessEncode(List<VarInfo> list)
        {
            string value = "\t\t";
            for (int i = 0; i < list.Count; i++)
            {
                value += "e(data,this->" + list[i].value.Replace(";", "") + ");\n\t\t";
            }
            value = value.Remove(value.LastIndexOf("\n"));
            return value;
        }
        static string ProcessDecode(List<VarInfo> list)
        {
            string value = "\t\t";
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type.StartsWith("TArray<"))
                {
                    value += "d(data,this->" + list[i].value.Replace(";", "") + ");\n\t\t";
                }
                else
                {
                    value += "d(data,&this->" + list[i].value.Replace(";", "") + ");\n\t\t";
                }
            }
            value = value.Remove(value.LastIndexOf("\n"));
            return value;
        }

        static void LineProcess(string line, ref string info, ref List<VarInfo> list)
        {
            if (line == string.Empty) return;
            if (line.StartsWith("//"))
            {
                info += line + "\n\t";
                return;
            }
            string[] values = line.Split(' ');
            if (values.Length != 2) return;
            VarInfo vi = new VarInfo();
            vi.about = info;
            vi.type = values[0].Trim();
            switch (vi.type)
            {
                case "bool": break;
                case "int": break;
                case "short":  break;
                case "long": vi.type = "int64"; break;
                case "float":  break;
                case "double":  break;
                case "string": vi.type = "FString"; break;
                case "byte": vi.type = "uint8"; break;
                case "bool[]":
                    vi.type = "TArray<bool>";
                    break;
                case "int[]": vi.type = "TArray<int>"; break;
                case "byte[]": vi.type = "TArray<uint8>"; break;
                case "short[]": vi.type = "TArray<short>"; break;
                case "long[]": vi.type = "TArray<int64>"; break;
                case "float[]": vi.type = "TArray<float>"; break;
                case "double[]": vi.type = "TArray<double>"; break;
                case "string[]":
                    vi.type = "TArray<FString>";
                    break;
                default:
                    //序列化对象在UE4中都强制在前面添加F 否则不符合UE4命名规范
                    if (vi.type.IndexOf("[]") > 0)
                    {
                        vi.type = "TArray<F" + vi.type.Replace("[]", "")+">";
                    }
                    else
                    {
                        vi.type = "F" + vi.type;
                    }
                    break;
            }


            vi.value = values[1].Trim();
            list.Add(vi);
            info = "\n\t";
        }
    }
}
