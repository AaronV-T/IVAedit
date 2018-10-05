using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class MathHelper
  {
    public static int GCD(int[] numbers)
    {
      return numbers.Aggregate(GCD);
    }

    public static int GCD(int a, int b)
    {
      return b == 0 ? a : GCD(b, a % b);
    }
  }
}
