# add below to update
# dotnet tool uninstall --global wyam.tool
dotnet tool install --global wyam.tool
# iex '"c:\Program Files (x86)\Google\Chrome\Application\chrome.exe" "http://localhost:5080"'
iex 'wyam build --preview --watch'