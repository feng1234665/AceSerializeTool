
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DTOCreator
{

    public partial class Form1 : Form
    {
        string classStr = @"using org.ace;
public struct ClassName : ISerialize
{
##vars
##Constructor

    public void decode(byte[] data)
    {
        ByteArray array = new ByteArray(data);
        decode(ref array);
    }
    
    public void decode(ref ByteArray data)
    {
##debuild
    }

    public byte[] encode()
    {
        ByteArray array = new ByteArray();
##build
        return array.Buffer;
    }
}";
        public Form1()
        {
            InitializeComponent();
        }
        public void BuildCS()
        {
            string result = classStr.Replace("ClassName", ClassNameBox.Text.Trim());
            string[] vars = VarBox.Text.Split('\n');
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
                varString += infos[i].about + "public " + infos[i].type + " " + infos[i].value + "\n";
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
            File.WriteAllText(ClassNameBox.Text.Trim() + ".cs", result);
            MessageBox.Show("导出" + ClassNameBox.Text.Trim() + ".cs成功");
        }

        string ProcessEncode(List<VarInfo> list)
        {
            string value = "\t\t";
            for (int i = 0; i < list.Count; i++)
            {
                value += "MiniEncode.e(ref array,this." + list[i].value.Replace(";", "") + ");\n\t\t";
            }
            value = value.Remove(value.LastIndexOf("\n"));
            return value;
        }
        string ProcessDecode(List<VarInfo> list)
        {
            string value = "\t\t";
            for (int i = 0; i < list.Count; i++)
            {
                value += "MiniEncode.d(ref data,out this." + list[i].value.Replace(";", "") + ");\n\t\t";
            }
            value = value.Remove(value.LastIndexOf("\n"));
            return value;
        }

        void LineProcess(string line, ref string info, ref List<VarInfo> list)
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
            vi.value = values[1].Trim();
            list.Add(vi);
            info = "\n\t";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BuildCS();
        }


        private void button3_Click(object sender, EventArgs e)
        {
            BuildGo();
        }
        string goStr = @"
package ##pak
import (
	""AceFrameWork/Serialize""
)
type ##ClassName struct {
	Serialize.SerializeBase
	##vars
}

func (this *##ClassName) Encode() []byte {
    this.BeginWrite()
	##build
	return this.EndWrite()
}
func (this *##ClassName) Decode(data []byte) error {
    var err error
	this.BeginRead(data)
    defer this.EndRead()
	##debuild    
	return err
}";
        void BuildGo()
        {
            string pak = textBox1.Text.Trim();
            string result = "";
            if (pak.Equals(string.Empty))
            {
                result = goStr.Replace("##pak", "main");
            }
            else
            {
                result = goStr.Replace("##pak", pak);
            }
            result = result.Replace("##ClassName", ClassNameBox.Text.Trim());
            string[] vars = VarBox.Text.Split('\n');
            string info = "\n\t";
            List<VarInfo> infos = new List<VarInfo>();
            for (int i = 0; i < vars.Length; i++)
            {
                string line = vars[i].Trim().Replace("\r", "");
                GoLineProcess(line, ref info, ref infos);
            }
            string varString = "";
            for (int i = 0; i < infos.Count; i++)
            {
                varString += infos[i].about + infos[i].value + " " + infos[i].type + "\n";
            }
            result = result.Replace("##vars", varString);
            result = result.Replace("##build", GoProcessEncode(infos));
            result = result.Replace("##debuild", GoProcessDecode(infos));

            File.WriteAllText(ClassNameBox.Text.Trim() + ".go", result);
            MessageBox.Show("导出" + ClassNameBox.Text.Trim() + ".go成功");
        }
        void GoLineProcess(string line, ref string info, ref List<VarInfo> list)
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
                case "byte": break;
                case "short": vi.type = "int16"; break;
                case "long": vi.type = "int64"; break;
                case "float": vi.type = "float32"; break;
                case "double": vi.type = "float64"; break;
                case "string": break;
                case "bool[]":
                    vi.type = "[]bool";
                    break;
                case "int[]": vi.type = "[]int"; break;
                case "byte[]": vi.type = "[]byte"; break;
                case "short[]": vi.type = "[]int16"; break;
                case "long[]": vi.type = "[]int64"; break;
                case "float[]": vi.type = "[]float32"; break;
                case "double[]": vi.type = "[]float64"; break;
                case "string[]":
                    vi.type = "[]string";
                    break;
                default:
                    if (vi.type.IndexOf("[]") > 0)
                    {
                        vi.type = "[]" + vi.type.Replace("[]", "");
                    }
                    break;
            }
            vi.value = values[1].Trim().Replace(";", "");
            vi.value = char.ToUpper(vi.value[0]) + vi.value.Substring(1);
            list.Add(vi);
            info = "\n\t";
        }

        string GoProcessEncode(List<VarInfo> list)
        {
            string value = "";
            for (int i = 0; i < list.Count; i++)
            {
                switch (list[i].type)
                {
                    case "bool":
                        value += "this.Wbl(this." + list[i].value.Replace(";", "") + ");\n\t";
                        break;
                    case "int": value += "this.Wi(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "byte": value += "this.Wbyte(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "int16": value += "this.Wsh(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "int64": value += "this.Wlong(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "float32": value += "this.Wf(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "float64": value += "this.Wd(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "string": value += "this.Wstr(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]bool":
                        value += "this.Wabl(this." + list[i].value.Replace(";", "") + ");\n\t";
                        break;
                    case "[]int": value += "this.Wai(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]byte": value += "this.Wabyte(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]short": value += "this.Wash(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]long": value += "this.Walong(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]float": value += "this.Waf(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]double": value += "this.Wad(this." + list[i].value.Replace(";", "") + ");\n\t"; break;
                    case "[]string":
                        value += "this.Wastr(this." + list[i].value.Replace(";", "") + ");\n\t";
                        break;
                    default:
                        if (list[i].type.IndexOf("[]") > 0)
                        {
                            value += "this.Wastruct(this." + list[i].value.Replace(";", "") + ");\n\t";
                        }
                        else
                        {
                            value += "this.Wstruct(&this." + list[i].value.Replace(";", "") + ");\n\t";
                        }
                        break;
                }
            }
            value = value.Remove(value.LastIndexOf("\n\t"));
            return value;
        }
        string GoProcessDecode(List<VarInfo> list)
        {
            string value = "";
            for (int i = 0; i < list.Count; i++)
            {
                switch (list[i].type)
                {
                    case "bool":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rbl();\n\t";
                        break;
                    case "int":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Ri();\n\t";
                        break;
                    case "byte":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rbyte();\n\t";
                        break;
                    case "int16":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rsh();\n\t";
                        break;
                    case "int64":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rlong();\n\t";
                        break;
                    case "float32":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rf();\n\t";
                        break;
                    case "float64":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rd();\n\t";
                        break;
                    case "string":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rstr();\n\t";
                        break;
                    case "[]bool":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rabl();\n\t";
                        break;
                    case "[]int":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rai();\n\t";
                        break;
                    case "[]byte":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rabyte();\n\t";
                        break;
                    case "[]short":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rash();\n\t";
                        break;
                    case "[]long":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Ralong();\n\t";
                        break;
                    case "[]float":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Raf();\n\t";
                        break;
                    case "[]double":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rad();\n\t";
                        break;
                    case "[]string":
                        value += "this." + list[i].value.Replace(";", "") + ",err=this.Rastr();\n\t";
                        break;
                    default:
                        if (list[i].type.IndexOf("[]") > 0)
                        {
                            value += "err=this.Rastruct(&this." + list[i].value.Replace(";", "") + ");\n\t";
                        }
                        else
                        {
                            value += "err=this.Rstruct(&this." + list[i].value.Replace(";", "") + ");\n\t";
                        }
                        break;
                }
                value += "if err!=nil{return err}\n\t";
            }
            value = value.Remove(value.LastIndexOf("\n\tif err"));
            return value;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UE_CPP.Build(ClassNameBox.Text.Trim(), VarBox.Text, checkBox1.Checked, textBox1.Text.Trim());
        }
    }
}
