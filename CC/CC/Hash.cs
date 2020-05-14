/*

   Copyright (C) 2020. rollrat All Rights Reserved.

   Author: Jeong HyunJun

*/

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CC
{
    public static class Hash
    {
        public static string GetHashSHA1(this string str)
        {
            SHA1Managed sha = new SHA1Managed();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
}
