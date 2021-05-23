# Content Cross Poster

This action converts a series of files from a source format, ready for posting to a third party blogging service in another format. This action is executed in a .NET Console App through a docker container. All of the code for the .NET Console application is available within this repository, and can be used standalone aside from the GitHub Action if preferred.

The default experience is that the action will convert all files that match a specific filter, in a given directory (and subdirectories if recursive subdirectories is enabled). If you prefer to only convert and post the files that have changed in your most recent commit, you will need to ensure that the local file system (e.g. the GitHub Actions Runner) is in a suitable state.

## Initial wave of implementation:
* Source
  * Hugo Content in YAML Format
* Sink
  * DevTo
  * Medium

Additional source and sinks will be considered! However, to implement that, we'll need your experience and expertise. Please start by creating a GitHub issue requesting the addition, and we can continue the discussion there.

# Usage

This software is a pet side-project and currently in active development. It is therefore not recommended for production use.

## GitHub Action Example

```yaml
- uses: chrisreddington/hugocrossposter@main
  with:
    # Directory path of the content to be converted and crossposted.
    # This is a required property.
    directoryPath: './content/'

    # Boolean (True/False) on whether Recursive Subdirectories should be used for file access
    # This is a required property.
    #
    # Default: false
    recursiveSubdirectories: 'true'

    # Boolean (True/False) on whether the details of the original post (date/time, and canonical URL) should be included in the rendered markdown.
    # This is not a required property.
    #
    # Default: false
    originalPostInformation: 'false'

    # Boolean (True/False) on whether the output of the payload should also be outputted in the logs.
    # This is not a required property.
    #
    # Default: false
    logPayloadOutput: 'false'

    # The search string to match against the names of files in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters
    # but it # doesn't support regular expressions. Defaults to *.md.
    # This is not a required property.
    #
    # Default: '*.md'
    searchPattern: '*.md*'

    # Base URL of the canonical source content's website, not including protocol. e.g. www.cloudwithchris.com. 
    # This is used for converting any relative links to the original source # including the canonical URL.
    # This is a required property.
    #
    # Default: 'www.cloudwithchris.com'
    baseUrl: 'www.yourblog.com'

    # DevTo Integration Token. This is required if crossposting to DevTo, as it forms part of the URL for the API Call.
    # This is not a required property.
    devtoToken: ''
    
    # DevTo Organization. If you are posting as a user and want to associate the post with an organization, enter the organization ID (not username) here.
    # This is not a required property.
    devToOrganization: ''

    # Medium Author ID. This is required if crossposting to medium, as it forms part of the URL for the API Call.
    # This is not a required property.
    mediumAuthorId: ''

    # Medium Integration Token. This is required to authorize to the Medium API.
    # This is not a required property.
    mediumToken: ''

    # Protocol used on the canonical source content's website, so that external links use the appropriate protocol.
    # Options are either HTTP or HTTPS. This is used for converting any relative links to the original source, including the canonical URL.
    # This is not a required property.
    protocol: 'http'
```

## GitHub Action Scenarios

** Coming Soon **

## Standalone .NET Command Line Execution

The cross poster is a .NET Core Command Line application, so can be used outside of GitHub actions if preferred. You will require a version of the .NET Core SDK on your local environment, and will first need to restore any required dependencies. From that point, you should be able to either run the application in debug mode or build the application.

To run the application, find the generated dll in the /bin/ subdirectory (depending upon which mode you built the application). Run the application using ``dotnet HugoCrossPoster.dll``. 

You will notice that there are several flags (inputs) required to successfully execute the application. These are clearly documented within program.cs.

## Contributing
This project welcomes contributions and suggestions. When you submit code changes, your submissions will be understood under the same [MIT License](https://github.com/chrisreddington/HugoCrossPoster/blob/main/LICENSE) that covers the project.

For detailed guidelines, including Feature Requests, Bug Reports and Code Contributions visit the [Contributing Guide](https://github.com/chrisreddington/HugoCrossPoster/blob/main/CONTRIBUTING.md).

## Support
If you like this project, I'd greatly appreciate your support for my blog/podcast/vlog, Cloud With Chris.

* [Apple Podcasts]( https://podcasts.apple.com/gb/podcast/cloud-with-chris/id1499633784)
* [Google Podcasts](https://podcasts.google.com/feed/aHR0cHM6Ly93d3cuY2xvdWR3aXRoY2hyaXMuY29tL2VwaXNvZGUvaW5kZXgueG1s?sa=X&ved=0CAMQ4aUDahcKEwiwsr2N1ePtAhUAAAAAHQAAAAAQBA)
* [PocketCasts](https://pca.st/u5t985sn)
* [RSS](https://www.cloudwithchris.com/episode/index.xml)
* [Spotify](https://open.spotify.com/show/3oBrdKm5grzl58GBiV0j2y)
* [Stitcher](https://www.stitcher.com/s?fid=507667&refid=stpr)
* [Twitter](https://www.twitter.com/reddobowen)
* [YouTube](https://www.youtube.com/c/CloudWithChris)
* [Website](https://www.cloudwithchris.com)

# License
The scripts and documentation in this project are released under the [MIT License](LICENSE)