/*

   Copyright (C) 2020. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC.Code
{
    public class LPType
    {
        public enum TypeOption
        {
            t_notdetermine,
            t_void,
            t_integer,
            t_bool,
            t_float,
            t_double,
            t_string,
            t_array,
            t_function,
        }
        
        public TypeOption Type { get; set; }
        public bool IsArray { get; set; }
        public LPType ArrayType { get; set; }
        public List<LPType> FunctionArgumentType { get; set; }
    }
}
