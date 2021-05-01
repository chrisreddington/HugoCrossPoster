using System;
using Xunit;
using HugoCrossPoster.Services;
using System.Linq;
using System.Collections.Generic;

namespace HugoCrossPoster.Tests
{
    public class ConvertFromMarkdownServiceTests
    {

        private ConvertFromMarkdownService markdownService = new ConvertFromMarkdownService();
        private string exampleMarkdown = @"tags:
        - tag1
        - tag 2";

        [Fact]
        public async void AssertTagsLengthAndValuesCorrectWhenLowerThanCount()
        {

            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags");
            //Assert.Equal(2, (await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags")).Count());
            Assert.Equal(2, tags.Count());
            Assert.Equal("tag1", tags[0]);
            Assert.Equal("tag 2", tags[1]);
        }


        [Fact]
        public async void AssertTagsLengthAndVAluesCorrectWhenCountLessThanTags()
        {

            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags", 1);
            //Assert.Equal(2, (await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags")).Count());
            Assert.Single(tags);
            Assert.Equal("tag1", tags[0]);
        }
    }
}
