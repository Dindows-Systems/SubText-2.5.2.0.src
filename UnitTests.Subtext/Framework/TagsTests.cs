using System.Collections.Generic;
using System.Linq;
using MbUnit.Framework;
using Subtext.Framework;
using Subtext.Framework.Components;
using Subtext.Framework.Providers;
using System;

namespace UnitTests.Subtext.Framework
{
    [TestFixture]
    public class TagsTests
    {
        [RowTest]
        [Row(-1, 1, 1)]
        [Row(0, 1, 2)]
        [Row(.25, 1, 3)]
        [Row(.49, 1, 4)]
        [Row(.9, 1, 5)]
        [Row(1.9, 1, 6)]
        [Row(2, 1, 7)]
        public void CanComputeWeight(double factor, double stdDev, int expected)
        {
            Assert.AreEqual(expected, Tags.ComputeWeight(factor, stdDev));
        }

        [Test]
        public void GetTopTagsThrowsArgumentExceptionForNegativeValues()
        {
            UnitTestHelper.AssertThrows<ArgumentException>(() =>ObjectProvider.Instance().GetMostUsedTags(-1));
        }

        [Test]
        [RollBack2]
        public void GetGetTopTags()
        {
            UnitTestHelper.SetupBlog();

            Entry entry = UnitTestHelper.CreateEntryInstanceForSyndication("test", "the title for this post", "test");
            UnitTestHelper.Create(entry);
            Tags.SetTagsOnEntry(entry.Id, new List<string>(new[] {"tag1", "tag2", "tag3"}));

            entry = UnitTestHelper.CreateEntryInstanceForSyndication("test", "the title for this post",
                                                                     @"<a href=""http://blah/tag3/"" rel=""tag"">test</a>");
            UnitTestHelper.Create(entry);

            ICollection<Tag> topTags = ObjectProvider.Instance().GetMostUsedTags(1);
            Assert.AreEqual("tag3", topTags.First().TagName);
        }
    }
}