using Xunit;
using HugoCrossPoster.Services;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HugoCrossPoster.Tests
{
    public class ConvertFromMarkdownServiceTests
    {

        private ConvertFromMarkdownService markdownService = new ConvertFromMarkdownService();
        private string exampleMarkdown = @"tags:
        - tag1
        - tag 2";

        [Fact]
        public async Task AssertTagsLengthAndValuesAreCorrectWhenCountOfTagsIsLowerThanTakeValue()
        {
            // Arrange is carried out already

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags");

            // Assert
            await Task.Run(() => Assert.Equal(2, tags.Count()));
            await Task.Run(() => Assert.Equal("tag1", tags[0]));
            await Task.Run(() => Assert.Equal("tag 2", tags[1]));
        }

        [Fact]
        public async Task AssertTagsLengthAndValuesAreCorrectWhenCountIsLessThanTags()
        {
            // Arrange is carried out already

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdown, "tags", 1);

            // Assert
            await Task.Run(() => Assert.Single(tags));
            await Task.Run(() => Assert.Equal("tag1", tags[0]));
        }

        [Fact]
        public async Task AssertRegexWorksWhenThereAreSpacesAfterPropertyName()
        {
            // Arrange
            string exampleMarkdownWithSpace = @"key: 
            - tag1
            - tag 2";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownWithSpace, "key");

            // Assert
            await Task.Run(() => Assert.Equal(2, tags.Count()));
            await Task.Run(() => Assert.Equal("tag1", tags[0]));
            await Task.Run(() => Assert.Equal("tag 2", tags[1]));
        }

        [Fact]
        public async Task AssertRegExWorksWhenThereAreSpecialSymbolsInName()
        {
            // Arrange
            string exampleMarkdownWithSymbols = @"key: 
            - cloudwithchris.com
            - hybrid-cloud";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownWithSymbols, "key");

            // Assert
            await Task.Run(() => Assert.Equal(2, tags.Count()));
            await Task.Run(() => Assert.Equal("cloudwithchris.com", tags[0]));
            await Task.Run(() => Assert.Equal("hybrid-cloud", tags[1]));
        }

        [Fact]
        public async Task AssertRegExCorrectlyIdentifiesDanglingKey()
        {
            // Arrange
            string exampleMarkdownWithNoValuesInList = @"key:
            anotherkey:";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownWithNoValuesInList, "key");

            // Assert
            await Task.Run(() => Assert.Empty(tags));
        }

        [Fact]
        public async Task AssertRegExDoesntPickUpStringListOfValues()
        {
            // Arrange
            string exampleMarkdownList = @"key: one, two, three, four";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownList, "key");

            // Assert
            await Task.Run(() => Assert.Empty(tags));
        }

        [Fact]
        public async Task AssetFrontMatterKeyValueIsInsenstiveInGetFrontMatterPropertyList()
        {
            // Arrange
            string exampleMarkdownList = @"KeY: one, two, three, four";

            // Act
            List<string> tags = await markdownService.getFrontMatterPropertyList(exampleMarkdownList, "key");

            // Assert
            await Task.Run(() => Assert.Empty(tags));
        }

        [Fact]
        public async Task AssetFrontMatterKeyValueIsInsenstiveInGetFrontMatterProperty()
        {
            // Arrange
            string exampleMarkdownList = @"Title: My Cool Blog Title";

            // Act
            string value = await markdownService.getFrontmatterProperty(exampleMarkdownList, "title");

            // Assert
            await Task.Run(() => Assert.Equal("My Cool Blog Title", value));
        }

        [Fact]
        public async Task AssertRegExDisplaysPropertyValueCorrectly()
        {
            // Arrange
            string exampleMarkdownList = @"title: My Cool Blog Title";

            // Act
            string value = await markdownService.getFrontmatterProperty(exampleMarkdownList, "title");

            // Assert
            await Task.Run(() => Assert.Equal("My Cool Blog Title", value));
        }

        [Fact]
        public async Task AssertHttpsURLsAreNotChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](https://www.google.com).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            await Task.Run(() => Assert.Equal("This is some content. [Here is a link](https://www.google.com).", value));
        }


        [Fact]
        public async Task AssertHttpURLsAreNotChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](http://www.google.com).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            await Task.Run(() => Assert.Equal("This is some content. [Here is a link](http://www.google.com).", value));
        }

        [Fact]
        public async Task AssertLocalURLsWithForwardSlashAreChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](/blog/post).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            await Task.Run(() => Assert.Equal($"This is some content. [Here is a link]({baseURL}/blog/post).", value));
        }

        [Fact]
        public async Task AssertLocalURLsWithoutForwardSlashAreChanged()
        {
            // Arrange
            string exampleMarkdownContent = @"This is some content. [Here is a link](blog/post).";
            string baseURL = "www.cloudwithchris.com";

            // Act
            string value = await markdownService.replaceLocalURLs(exampleMarkdownContent, baseURL);

            // Assert
            await Task.Run(() => Assert.Equal($"This is some content. [Here is a link]({baseURL}/blog/post).", value));
        }

        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFolderNonRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases", "*.md", false)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(15, numberOfFiles));
        }
        
        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFolderNonRecursiveWithIncorrectFilter()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases", "*.txt", false)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(0, numberOfFiles));
        }

        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFolderRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases", "*.md", true)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(20, numberOfFiles));
        }

        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFolderRecursiveWithIncorrectFilter()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases", "*.txt", true)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(0, numberOfFiles));
        }

        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFileNonRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases/test1.md", "*.md", false)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(0, numberOfFiles));
        }

        
        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFileNonRecursiveMismatchPatternFileExtension()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases/test1.md", "*.txt", false)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(0, numberOfFiles));
        }

        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFileRecursive()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases/test1.md", "*.md", true)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(0, numberOfFiles));
        }

        [Fact]
        public async Task AssertNumberOfFilesReadFromTestCasesFileRecursiveMismatchPatternFileExtension()
        {
            // Arrange - Complete, as files are already in place in repo.

            // Act
            int numberOfFiles = (await markdownService.listFiles("./testcases/test1.md", "*.txt", true)).Count();

            // Assert
            await Task.Run(() => Assert.Equal(0, numberOfFiles));
        }


        [Fact]
        public async Task AssertReadFileCorrectlyReadsFile()
        {
            // Arrange
            string exampleMarkdown1Contents = 
            @"---
            Author: chrisreddington
            Description: ""Test 1""
            - img/cloudwithchrislogo.png
            TITLE: 'Test1'
            youtube: okaSk5QxeJk
            tags:
            - azure
            - cloud
            - devops
            - github
            series:
            - Cloud Drops

            ---
            # Hello
            This is test 1";

            string file = exampleMarkdown1Contents.Replace("\r\n", "\n").Replace("            ","");

            // Act
            string fileContents = await markdownService.readFile("./testcases/test1.md");

            // Assert
            await Task.Run(() => Assert.Equal(file, fileContents));
        }

        [Fact]
        public async Task AssertThrowsExceptionOnNonExistingFile()
        {
            // Arrange - already done as file exists in git repo.

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(async () => await markdownService.readFile("./testcases/nonexistentfile.md"));
        }

        [Fact]
        public async Task AssertCorrectHTTPCanonicalURLForMainDirectory()
        {
            // Arrange - already done as file exists in git repo.
            string expectedUrl = "http://www.cloudwithchris.com/testcases/test1";

            // Act
            string resultUrl = await markdownService.getCanonicalUrl("http", "www.cloudwithchris.com", "testcases/test1.md");

            // Assert
            await Task.Run(() => Assert.Equal(expectedUrl, resultUrl));
        }

        [Fact]
        public async Task AssertCorrectHTTPSCanonicalURLForMainDirectory()
        {
            // Arrange - already done as file exists in git repo.
            string expectedUrl = "https://www.cloudwithchris.com/testcases/test1";

            // Act
            string resultUrl = await markdownService.getCanonicalUrl("https", "www.cloudwithchris.com", "testcases/test1.md");

            // Assert
            await Task.Run(() => Assert.Equal(expectedUrl, resultUrl));
        }

        [Fact]
        public async Task AssertCorrectHTTPCanonicalURLForMainDirectorySubfolder()
        {
            // Arrange - already done as file exists in git repo.
            string expectedUrl = "http://www.cloudwithchris.com/testcases/subfolder/test5";

            // Act
            string resultUrl = await markdownService.getCanonicalUrl("http", "www.cloudwithchris.com", "testcases/subfolder/test5.md");

            // Assert
            await Task.Run(() => Assert.Equal(expectedUrl, resultUrl));
        }


        [Fact]
        public async Task AssertCorrectHTTPSCanonicalURLForMainDirectorySubfolder()
        {
            // Arrange - already done as file exists in git repo.
            string expectedUrl = "https://www.cloudwithchris.com/testcases/subfolder/test5";

            // Act
            string resultUrl = await markdownService.getCanonicalUrl("https", "www.cloudwithchris.com", "testcases/subfolder/test5.md");

            // Assert
            await Task.Run(() => Assert.Equal(expectedUrl, resultUrl));
        }


        [Fact]
        public async Task AssertRemoveFrontMatterRemovesDetailsCorrectly()
        {
            // Arrange
            string exampleMarkdown1Contents =
            @"---
            Author: chrisreddington
            Description: ""Test 1""
            - img/cloudwithchrislogo.png
            TITLE: 'Test1'
            youtube: okaSk5QxeJk
            tags:
            - azure
            - cloud
            - devops
            - github
            series:
            - Cloud Drops

            ---
            # Hello
            This is test 1";

            string expectedResult =
            @"# Hello
            This is test 1";

            // Act
            string fileContents = await markdownService.removeFrontMatter(exampleMarkdown1Contents);

            // Assert
            await Task.Run(() => Assert.Equal(expectedResult, fileContents));
        }
    }
}
