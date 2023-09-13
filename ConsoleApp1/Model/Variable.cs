using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Model
{
    public class Variable
    {
        public string name { get; set; }
        public string address { get; set; }
        public Variable( string name ,string address)
        {
            this.name = name;
            this.address = address;
        }
    }
    
}
