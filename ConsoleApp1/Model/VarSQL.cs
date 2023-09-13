using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Model
{
    public class VarSQL
    {
        public string name { get; set; }
        public double value { get; set; }
        public DateTime data { get; set; }
        public VarSQL(string name , double value) {
            this.name = name;
            this.value = value;
            this.data = DateTime.Now;
        }
        public override string ToString()
        {
            return string.Format("Name = {0}, Value = {1}, data = {2}", name, value, data);
        }

    }
}
