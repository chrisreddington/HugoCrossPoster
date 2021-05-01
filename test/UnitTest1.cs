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
        public async void AssertTagsLengthAndValuesAreCorrectWhenCountOfTagsIsLowerThanTakeValue()
        {
            // Arrange is carried out already

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags");

            // Assert
            Assert.Equal(2, tags.Count());
            Assert.Equal("tag1", tags[0]);
            Assert.Equal("tag 2", tags[1]);
        }

        [Fact]
        public async void AssertTagsLengthAndValuesAreCorrectWhenCountIsLessThanTags()
        {
            // Arrange is carried out already

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags", 1);

            // Assert
            Assert.Single(tags);
            Assert.Equal("tag1", tags[0]);
        }

        [Fact]
        public async void AssertRegexWorksWhenThereAreSpacesAfterPropertyName()
        {
            // Arrange
            string exampleMarkdownWithSpace = @"key: 
            - tag1
            - tag 2";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownWithSpace, "key");

            // Assert
            Assert.Equal(2, tags.Count());
            Assert.Equal("tag1", tags[0]);
            Assert.Equal("tag 2", tags[1]);
        }

        [Fact]
        public async void AssertRegExWorksWhenThereAreSpecialSymbolsInName()
        {
            // Arrange
            string exampleMarkdownWithSymbols = @"key: 
            - cloudwithchris.com
            - hybrid-cloud";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownWithSymbols, "key");

            // Assert
            Assert.Equal(2, tags.Count());
            Assert.Equal("cloudwithchris.com", tags[0]);
            Assert.Equal("hybrid-cloud", tags[1]);
        }

        [Fact]
        public async void AssertRegExCorrectlyIdentifiesDanglingKey()
        {
            // Arrange
            string exampleMarkdownWithNoValuesInList = @"key:
            anotherkey:";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownWithNoValuesInList, "key");

            // Assert
            Assert.Empty(tags);
        }

        [Fact]
        public async void AssertRegExDoesntPickUpStringListOfValues()
        {
            // Arrange
            string exampleMarkdownList = @"key: one, two, three, four";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownList, "key");

            // Assert
            Assert.Empty(tags);
        }

        [Fact]
        public async void AssetFrontMatterKeyValueIsInsenstiveInGetFrontMatterPropertyList()
        {
            // Arrange
            string exampleMarkdownList = @"KeY: one, two, three, four";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownList, "key");

            // Assert
            Assert.Empty(tags);
        }

        [Fact]
        public async void AssetFrontMatterKeyValueIsInsenstiveInGetFrontMatterProperty()
        {
            // Arrange
            string exampleMarkdownList = @"Title: My Cool Blog Title";

            // Act
            string value = await markdownService.getFrontmatterProperty(exampleMarkdownList, "title");

            // Assert
            Assert.Equal("My Cool Blog Title", value);
        }

        [Fact]
        public async void AssertRegExDisplaysPropertyValueCorrectly()
        {
            // Arrange
            string exampleMarkdownList = @"title: My Cool Blog Title";

            // Act
            string value = await markdownService.getFrontmatterProperty(exampleMarkdownList, "title");

            // Assert
            Assert.Equal("My Cool Blog Title", value);
        }

        [Fact]
        public async void AssertHttpsURLsAreNotChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](https://www.google.com).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            Assert.Equal("This is some content. [Here is a link](https://www.google.com).", value);
        }


        [Fact]
        public async void AssertHttpURLsAreNotChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](http://www.google.com).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            Assert.Equal("This is some content. [Here is a link](http://www.google.com).", value);
        }

        [Fact]
        public async void AssertLocalURLsWithForwardSlashAreChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](/blog/post).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            Assert.Equal($"This is some content. [Here is a link]({baseURL}/blog/post).", value);
        }

        [Fact]
        public async void AssertLocalURLsWithoutForwardSlashAreChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](blog/post).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            Assert.Equal($"This is some content. [Here is a link]({baseURL}/blog/post).", value);
        }

        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFolderNonRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases", "*.md", false)).Count();

            // Assert
            Assert.Equal(15, numberOfFiles);
        }
        
        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFolderNonRecursiveWithIncorrectFilter()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases", "*.txt", false)).Count();

            // Assert
            Assert.Equal(0, numberOfFiles);
        }

        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFolderRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases", "*.md", true)).Count();

            // Assert
            Assert.Equal(20, numberOfFiles);
        }

        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFolderRecursiveWithIncorrectFilter()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases", "*.txt", true)).Count();

            // Assert
            Assert.Equal(0, numberOfFiles);
        }

        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFileNonRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases/test1.md", "*.md", false)).Count();

            // Assert
            Assert.Equal(1, numberOfFiles);
        }

        
        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFileNonRecursiveMismatchPatternFileExtension()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases/test1.md", "*.txt", false)).Count();

            // Assert
            Assert.Equal(1, numberOfFiles);
        }

        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFileRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases/test1.md", "*.md", true)).Count();

            // Assert
            Assert.Equal(1, numberOfFiles);
        }

        [Fact]
        public async void AssertNumberOfFilesReadFromTestCasesFileRecursiveMismatchPatternFileExtension()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("../../../testcases/test1.md", "*.txt", true)).Count();

            // Assert
            Assert.Equal(1, numberOfFiles);
        }
    }
}
