using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GennyMcGenFace.Models
{
    public class GenOptions
    {
        public GenOptions()
        {
            WordsInStrings = 1;
            IntLength = 8;
        }

        /// <summary>
        /// How many words are generated in each string
        /// </summary>
        public decimal WordsInStrings { get; set; }

        /// <summary>
        /// How big should the random int's be
        /// </summary>
        public decimal IntLength { get; set; }

        /// <summary>
        /// returns the number of digit of the rng
        /// </summary>
        /// <returns></returns>
        public int GetMaxIntLength()
        {
            //I wasn't sure how to the math to make IntLength 5= 99999 to put as the max value for the random number generator.  This should work
            if (IntLength == 0) return 0;
            var digitPlaceHolder = "";

            for (var i = 0; i < IntLength; i++)
            {
                digitPlaceHolder += "9";
            }

            return Convert.ToInt32(digitPlaceHolder);
        }
    }
}