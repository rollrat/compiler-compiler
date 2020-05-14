/*

   Copyright (C) 2020. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.Collections.Generic;
using System.Text;

namespace json_parser
{
    public abstract class JValue
    {
        public abstract StringBuilder print(StringBuilder os, bool format = false, string indent = "");
    }

    public class JObject : JValue
    {
        public override StringBuilder print(StringBuilder os, bool format = false, string indent = "")
        {
            if (!format)
                os.Append('{');
            else
                os.Append("{\n");
            for (int i = 0; i < keyvalue.Count; i++)
            {
                var it = keyvalue[i];
                if (!format)
                {
                    os.Append(it.Key + ":");
                    it.Value.print(os);
                }
                else
                {
                    os.Append(indent + "  " + it.Key + ": ");
                    it.Value.print(os, true, indent + "  ");
                }
                if (i != keyvalue.Count - 1)
                {
                    if (!format)
                        os.Append(',');
                    else
                        os.Append(",\n");
                }
            }
            if (!format)
                os.Append('}');
            else
                os.Append('\n' + indent + "}");
            return os;
        }

        public List<KeyValuePair<string, JValue>> keyvalue = new List<KeyValuePair<string, JValue>>();
    }

    public class JArray : JValue
    {
        public override StringBuilder print(StringBuilder os, bool format = false, string indent = "")
        {
            if (array.Count > 0)
            {
                if (!format)
                    os.Append('[');
                else
                    os.Append("[\n");
                for (int i = 0; i < array.Count; i++)
                {
                    var it = array[i];
                    if (!format)
                        it.print(os);
                    else
                    {
                        os.Append(indent + "  ");
                        it.print(os, true, indent + "  ");
                    }
                    if (i != array.Count - 1)
                    {
                        if (!format)
                            os.Append(',');
                        else
                            os.Append(",\n");
                    }
                }
                if (!format)
                    os.Append(']');
                else
                    os.Append('\n' + indent + "]");
            }
            else
            {
                os.Append("[]");
            }
            return os;
        }

        public List<JValue> array = new List<JValue>();
    }

    public class JNumeric : JValue
    {
        public override StringBuilder print(StringBuilder os, bool format = false, string indent = "")
        {
            os.Append(numstr);
            return os;
        }

        public string numstr;
        public bool is_integer = false;
    }

    public enum JToken
    {
        none = 0,
        json_nt_json,
        json_nt_array,
        json_nt_object,
        json_nt_members,
        json_nt_pair,
        json_nt_elements,
        json_nt_value,
        object_starts,
        object_ends,
        v_comma,
        v_pair,
        array_starts,
        array_ends,
        v_true,
        v_false,
        v_null,
        v_string,
        v_number,
        eof,
        error,
    };

    public class JState : JValue
    {
        public override StringBuilder print(StringBuilder os, bool format = false, string indent = "")
        {
            switch (type)
            {
                case JToken.v_false:
                    os.Append("false");
                    break;

                case JToken.v_true:
                    os.Append("true");
                    break;

                case JToken.v_null:
                    os.Append("null");
                    break;
            }
            return os;
        }

        public JToken type;
    }

    public class JString : JValue
    {
        public override StringBuilder print(StringBuilder os, bool format = false, string indent = "")
        {
            os.Append(str);
            return os;
        }

        public string str;
    }

}
