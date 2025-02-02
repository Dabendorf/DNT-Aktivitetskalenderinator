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

#### FilterIds
* `types`: `320` (Fellestur), `321` (Arrangement), `322` (Kurs), `323` (Dugnad), `324` (Annet)
* `subtype`: see file [SubtypeFacets.md](SubtypeFacets.md)
* `duration`: `none` (Dagsaktivitet), `twothree` (2-3 dager), `foursix` (4-6 dager), `oversix` (Over 6 dager)
* `associations`: see file [AssociationFacets.md](AssociationFacets.md)
* `targetGroups`: `Barn`, `Fjellsportsinteresserte`, `Funksjonshemmede`, `Seniorer`, `Ungdom`, `Utviklingshemmede`, `Voksne`
* `organizergroups`: `454` (DNT Ung), `455` (Barnas Turlag), `456` (DNT Fjellsport), `457` (DNT Senior), `458` (Tilrettelagt), `459` (Vandregruppa), `471` (Ukategorisert)
* `levels`: `1` (Enkel), `2` (Middels), `3` (Krevende), `4` (Ekstra krevende)
* `areas`: see file [AreaFacets.md](AreaFacets.md)
* `municipalities`: see file [MunicipalityFacets.md](MunicipalityFacets.md)
* `startdate` and `enddate`: format `startdate=11.02.2025&enddate=21.02.2025`