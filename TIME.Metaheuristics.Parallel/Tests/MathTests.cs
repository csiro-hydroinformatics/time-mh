using TIME.Metaheuristics.Parallel.ExtensionMethods;
using NUnit.Framework;

namespace TIME.Metaheuristics.Parallel.Tests
{
    [TestFixture]
    public class MathTests
    {
        [Test]
        public void ClampTestInt()
        {
            Assert.That(3.Clamp(2,5), Is.EqualTo(3), "in range");
            Assert.That(10.Clamp(11,20), Is.EqualTo(11), "clamp to min");
            Assert.That(42.Clamp(10, 41), Is.EqualTo(41), "Clamp to max");
        }

        [Test]
        public void ClampTestRangeInversion()
        {
            Assert.That(4.Clamp(5,3), Is.EqualTo(4));
            Assert.That(3.Clamp(5,4), Is.EqualTo(4));
            Assert.That(5.Clamp(4,3), Is.EqualTo(4));
        }
       
    }
}
