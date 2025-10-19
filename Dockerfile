# Use .NET 8 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["ConvertPDFToWord/ConvertPDFToWord.csproj", "ConvertPDFToWord/"]
RUN dotnet restore "ConvertPDFToWord/ConvertPDFToWord.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/ConvertPDFToWord"
RUN dotnet publish -c Release -o /app/out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 10000
ENTRYPOINT ["dotnet", "ConvertPDFToWord.dll"]
