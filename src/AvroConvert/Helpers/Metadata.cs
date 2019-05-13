﻿namespace EhwarSoft.AvroConvert.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Constants;
    using Exceptions;

    public class Metadata
    {
        private readonly Dictionary<string, byte[]> _value;

        public Metadata()
        {
            _value = new Dictionary<string, byte[]>();
        }

        public void Add(string key, string value)
        {
            _value.Add(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        public int GetSize()
        {
            return _value.Count;
        }

        public Dictionary<string, byte[]> GetValue()
        {
            return _value;
        }
    }
}
