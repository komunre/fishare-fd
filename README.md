# fishare-fd
false-direct live file sharing system

# How to use

## Client

`mv config.txt.clean config.txt` (Change config if you want to connect to other server)

`dotnet run -c Release --project Fishare.Client`

## Server

`dotnet run -c Release --project Fishare.Server port` where "port" is your server port
