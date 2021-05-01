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
        public async void AssertTagsWhenTagsPresent()
        {
            //Assert.Equal(2, (await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags")).Count());
            Assert.Equal(2, (await markdownService.getTags(exampleMarkdown)).Count());
        }
    }
}
