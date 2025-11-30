# allah

2 apps, one extremely complicated ebay with crypto support + public api, and second one is less complicated but also stupidly complex xiaoping-express

ebay stack:

user service
user service probably should be a rest api, but 90% of other microservices use grpc and dont expose any methods for public, all use gateway, so to make it consistent
i prefer consistency over logic. user-service will be grpc microservice, yes it's overkill, but zhizn' igra, igray krasivo.
also i don't have domain model for user. i just use db model and moved it to domain, because write your fucking mappers by yourself, cumsock

    <OutputType>Exe</OutputType>
    <IsAspireHost>true</IsAspireHost>[New folder](../../Downloads/New%20folder)