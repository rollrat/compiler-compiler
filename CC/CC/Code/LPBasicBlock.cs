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
    public class LPBasicBlock
        : LPDefine
    {
        List<LPOperator> insts;

        public LPBasicBlock()
        {
            insts = new List<LPOperator>();
        }

        public List<LPOperator> Childs { get { return insts; } }

        public void Insert(LPOperator op)
        {
            insts.Add(op);
        }
    }
}
