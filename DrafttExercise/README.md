# **Draftt-debug-session-interview**
### Description
- This repository contains an implementation of the specification listed below, with PostgreSQL and rabbitMQ
	- There is an existing mongoDB client implementation as well
- Feel free to change **anything you want** in the code - changing the implementations, implementing it with a different databases/queuing service of your choosing, and adding any enhancement you would like
	- If you changed anything in the codebase - please share the changes with us before the debug session
- You should be comfortable with the codebase, running the code, debugging it and making changes
- To run the project:
	- Make sure docker, node, and typescript are installed
	- Run "docker compose up -d"
	- Run "npm install"
	- Run "tsc"
	- Run "npm run consume" - this will spin up the consumer process
	- Run "npm i -g" - to install the cli
	- Run "cityStreets X" - replacing X with the city of your choice
		- We suggest running it on the city "Itamar" for testing purposes (it is a city with a small amount of streets)
		- Some errors may occur during execution - this is ok, you are not required to debug them before the session

## Project specification:
We want to insert data about street names in israel to a database, using some kind of a queueing platform.\
The project will be implemented in nodejs + Typescript. \
The data is provided via the israeli gov.il api.
An implmenetation of a StreetsService class which provides you the data from the api exists inside the repository.\
The list of cities is provided inside of the cities.ts file.\
 - If you want to take a look at the raw data - https://data.gov.il/he/datasets/population_authority/321/1b14e41c-85b3-4c21-bdce-9fe48185ffca

The project will need to implement two services:
 - Publishing service - A service that will get the data from the StreetsService and publish it to the queuing platform.
 - Consuming service -  A service that will consume the data from the queuing service and persisit it to the database.

 Publishing service specification:
  - Will be activated by CLI, accepting a city name from the list of cities.
  - Will query the StreetsService for all streets of that city.
  - Will publish to the queueing platform the streets it needs to insert.

Consuming service specification:
  - Will consume from the messaging queue.
  - Will persist the streets data to the selected database.

The persisted streets need to contain all data from the api.
---
## Provided dependencies:
 Provided is a docker-compose file which contains all of the dependencies that you will need to complete the assignment.
 **You do not need to lift all of the services**, you can choose which you need as listed in the assignment specification.
 ### The docker-compose exposes the following services:
  - PostgreSQL
	- exposed on localhost port 5432
	- Adminer UI is exposed on localhost port 8089
		- Authentication:
			- System: PostgreSQL
			- Server: postgres
			- Username: postgres
			- Password: password
			- Database: postgres
  
   - RabbitMQ
	- backend exposed on port 5672
	- management UI exposed on port 15672
		- Authentication:
			- Username: guest
			- Password: guest
  
  - mongoDB (turned off by default)
  	- No-sql database, Version - 4.2
	- exposed on localhost port 27017
	- Can be connected to with studio 3t with no need for authentication - https://studio3t.com/download/

 - Redpanda (turned off by default)
	- Fully Kafka-api compatible data streaming platform
	- Kafka broker exposed on port 9092
	- UI exposed on port 8014 with no need for authentication

If you would like to use a different service for a database/queueing system feel free to do so, as long as they adhere to the assignement specifications


---

If you have any questions about the assignment you can send them to shachar@draftt.io
Good luck!
