# DNT-Aktivitetskalenderinator
The projects aim is to filter activities in the DNT-calendar as well as getting noticed about them

It also aims to find out how the API works and documenting it

## API
Running the API endpoint `https://www.dnt.no/api/activities` (GET) makes it possible to get a list of activities without using the tedious DNT-aktivitetskalender. It returns json object with the following top level attributes.

### JSON return arguments
* pageHits: List of activites on this page (see page and pageSize below)
* typeFacets: Hiking type {Fellestur, arrangement, kurs, dugnad, annet}
* subTypeFacets: Undertypes of hikes, very random, some useful ones (e.g. fottur, klatrekurs, opptur), mange random (e.g. 355, 372)
* durationFacets: Duration {dagsaktivitet, 2-3dager etc.}
* asssociationFacets: DNT-grupper (e.g DNT Sunnmøre, Bergen og Hordaland Turlag)
* targetGroupFacets: Target group (e.g. barn, funksjonshemmede, ungdom, voksne)
* organizerGroupFacets: Organiser groups (e.g. DNT ung, barnas turlag)
* levelFacets: Difficulty levels (enkel, middels, krevende, ekstra krevende)
* areaFacets: Different areas (e.g. Dovrefjell, Telemark, Arna)
* municipalityFacets: Municipality
* page: Index of result page
* totalMatching: Total number of all events matching the above Facets
* pageSize: The number of events per page
* pageCount: Number of pages of results, directly connected to totalMatching, pageSize and page

#### Not sure how to set these
* currentPageUrl
* activityPageList
* activityFilter
* latitude
* longitude
* category

### Running as URL parameters
It is possible to use the upper URL with URL parameters to filter the activites. This is a bit more nasty if you want to include complexer requests.

The complex objects like the facets have filterIds with their objects. These can be used as arguments, if multiple are applicable, use a comma in between

Examples are:
* 20 events per page, page 3 (events 41-60): https://www.dnt.no/aktivitetskalender/?page=3&pageSize=20
* only events with more than 6 days length: https://www.dnt.no/aktivitetskalender/?duration=oversix (`oversix` being the filterId in durationFacets)
* events with 2-3 days length by Askøy Turlag or Bergen og Hordaland turlag: https://www.dnt.no/aktivitetskalender?duration=twothree&associations=25195,24939
* only Enkel or Middels difficulty: https://www.dnt.no/aktivitetskalender?levels=1,2
* Padletur: https://www.dnt.no/aktivitetskalender?subtypes=346
* Bergen byfjellene vest and sentral: https://www.dnt.no/aktivitetskalender?areas=12220,12221
* By municipality, only events in Øygarden between 2025-02-11 and 2025-02-21: https://www.dnt.no/aktivitetskalender?municipalities=4626&startdate=11.02.2025&enddate=21.02.2025
* Hikes of Til Topps Bergen group, using search string: https://www.dnt.no/aktivitetskalender?organizergroups=471&q=til+topps+bergen

#### FilterIds
* `types`: `320` (Fellestur), `321` (Arrangement), `322` (Kurs), `323` (Dugnad), `324` (Annet)
* `subtypes`: see file [SubtypeFacets.md](DocumentationApi/SubtypeFacets.md)
* `duration`: `none` (Dagsaktivitet), `twothree` (2-3 dager), `foursix` (4-6 dager), `oversix` (Over 6 dager)
* `associations`: see file [AssociationFacets.md](DocumentationApi/AssociationFacets.md)
* `targetGroups`: `Barn`, `Fjellsportsinteresserte`, `Funksjonshemmede`, `Seniorer`, `Ungdom`, `Utviklingshemmede`, `Voksne`
* `organizergroups`: `454` (DNT Ung), `455` (Barnas Turlag), `456` (DNT Fjellsport), `457` (DNT Senior), `458` (Tilrettelagt), `459` (Vandregruppa), `471` (Ukategorisert)
* `levels`: `1` (Enkel), `2` (Middels), `3` (Krevende), `4` (Ekstra krevende)
* `areas`: see file [AreaFacets.md](DocumentationApi/AreaFacets.md)
* `municipalities`: see file [MunicipalityFacets.md](DocumentationApi/MunicipalityFacets.md)
* `startdate` and `enddate`: format `startdate=11.02.2025&enddate=21.02.2025`
* `q`: String search, like `q=til+topps+bergen`

## The automation project
The project is written in C# and runs via `dotnet`. It is a one time run creating a database. Every time you run it and there is a new tour which hasn't been there before, it adds them and sends an overview over new tours to you via email.

## Install
Make sure to have a valid version of dotnet and sqlite installed on your computer.

## Secrets.json
The programme wants you to create a `secrets.json` file into the `DNTkalenderinator` folder including data about your email preferences, the email server etc. This makes it possible to send out new emails. If do not want this function, comment out the email function in the `Program.cs`.

Format of the `secrets.json`
```
{
	"SmtpServer": "smtp.yourserver.no",
	"SmtpPort": 587,
	"UseSsl": false,
	"Username": "your@sendingemail.adress",
	"Password": "Password",
	"FromName": "DNT Notifier",
	"FromEmail": "your@sendingemail.adress",
	"ToName": "Receiving name",
	"ToEmail": "your@receivingemail.adress",
	"Subject": "Nye turer funnet"
}
```

### Choose hikes
You have to provide a `queries.csv` file in the `DNTkalenderinator` folder providing a list of search queries and description pairs.

Example:
```
Description,Query
"[ALL]","&levels=2,3,4&targetGroups=Fjellsportsinteresserte,Ungdom,Voksne&types=320,324&subtypes=345,361,328,348,352,346,452&startdate=20.04.2025"
"2-3 dager Bergen og Askøy","duration=twothree&associations=25195,24939"
"Bergen byfjellene vest and sentral","areas=12220,12221"
```
There is an optional all-kvantor which is applied to all strings, being denoted with [ALL]. The [EXCLUDE] parameter filters away words included in the title, so please be very cautious here.

### Run project
`dotnet run` (after having run `dotnet build`)

### Docker
Docker is a piece of shit and doesn't work. There is a pipeline for building docker inside this project, you are welcome to fix it.

The following commands for pulling and running it on various systems my help you.
```sudo docker pull --platform linux/amd64 markusklingerdolvik/dntkalenderinator:latest
sudo docker run --rm -v  /home/pi/Bots/DNTAktivitetskalenderinator/DNTkalenderinator:/app markusklingerdolvik/dntkalenderinator:latest

docker pull --platform linux/arm64 markusklingerdolvik/dntkalenderinator:latest
docker run --rm -v --platform linux/arm64 /home/pi/Bots/DNTAktivitetskalenderinator/DNTkalenderinator:/app markusklingerdolvik/dntkalenderinator:latest
```

## Run on a raspberry pi automatically
If you have the desire to run this project automatically on a server (raspberry pi), to finally automate this with weekly cronjobs, go through these steps

### Install dotnet
see [Official guide](https://learn.microsoft.com/en-us/dotnet/iot/deployment)
```
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel STS
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.zshrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.zshrc
```
If dotnet 9 is not supported, install an older version
```
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.407
```

### Build and add files
Run `dotnet build` and add the `secrets.json` and `queries.csv` in your root folder of the project (as named above)

### Make cronjob
You may add this line to your cronjob file by running `crontab -e`
```
 0 0 * * 0 cd /home/pi/Bots/DNTAktivitetskalenderinator/DNTkalenderinator && /home/pi/.dotnet/dotnet run --project /home/pi/Bots/DNTAktivitetskalenderinator/DNTkalenderinator/DNTkalenderinator.csproj
 ```

 The path of the dotnet installation (`whereis dotnet`) and the project must be changed as well. Good luck.


## Vestlandet
This chapter is a collection of search-queries for the `queries.csv` serving people, who want to hike in Norway's most exiting region, Vestlandet
```
Description,Query
"[ALL]","&levels=3,4&targetGroups=Fjellsportsinteresserte,Ungdom,Voksne&types=320,324&subtypes=345,361,328,348,352,346,452&startdate=20.04.2025"
"[EXCLUDE]","Tysdagstur dagtid,Friskus,Ostrimmen,Treningstur,AKTIV: Dagstur,AKTIV: Kveldstur,Avlyst,Tirsdagstur"
"Hordaland","&municipalities=4601,4602,4612,4613,4614,4615,4616,4617,4618,4620,4621,4622,4624,4626,4628,4631"
"Bergen and surroundings area","&areas=12174,12211,12218,12220,12221,1266,1238,1239,1240"
"Vestlandske turlag","&associations=25195,25114,25116,24939,25068,24876,24904,25197,25107,25205,25238,26219,25117,26200,26202,27287,25504,26346,24901,25419,25874,25111,25118,25119,26220,25108,25120,24897,25206,25199,25130,25122,24931,26086,25123,25210,25200,25193,25198,25113,25112,25207,25232,25105,25124,26201,25201,24873,25194,25110,25109,25202,25204,25203,25125,25233,24898,25127,25128,25129"
"Stringsearch: Hardanger","&q=Hardanger"
"Stringsearch: Hardangervidden","&q=Hardangervidda"
"Stringsearch: Folgefonna","&q=Folgefonna"
"Stringsearch: Stølsheimen","&q=Stølsheimen"
"Stringsearch: Finse","&q=Finse"
```

The list includes a couple of standard filters
* Only non-easy tours: `levels=3,4`
* No senior or Barnas turlag: `organizergroups=454,456,459,471` or `targetGroups=Fjellsportsinteresserte,Ungdom,Voksne`
* No Arrangement, Kurs og Dugnad: `types=320,324`
* Only hikes, sykling tours and water activities: `subtypes=345,361,328,348,352,346,452`
* `&levels=3,4&targetGroups=Fjellsportsinteresserte,Ungdom,Voksne&types=320,324&subtypes=345,361,328,348,352,346,452`