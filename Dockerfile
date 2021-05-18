# Set the base image as the .NET 5.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./src/HugoCrossPoster.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Chris Reddington <chris@cloudwithchris.com>"
LABEL repository="https://github.com/chrisreddington/HugoCrossPoster"
LABEL homepage="https://github.com/chrisreddington/HugoCrossPoster"

# Label as GitHub action
LABEL com.github.actions.name="Hugo Cross Poster"
# Limit to 160 characters
LABEL com.github.actions.description="This is a work in progress .NET Core Console App to ease cross posting from Hugo to alternate formats."
# See branding:
# https://docs.github.com/actions/creating-actions/metadata-syntax-for-github-actions#branding
LABEL com.github.actions.icon="activity"
LABEL com.github.actions.color="orange"

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY --from=build-env /out .
ENTRYPOINT [ "dotnet", "/HugoCrossPoster.dll" ]