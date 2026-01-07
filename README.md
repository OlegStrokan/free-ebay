# allah

2 apps, one extremely complicated ebay with crypto support + public api, and second one is less complicated but also stupidly complex xiaoping-express

ebay stack:

Tech info (not complete)
user service

user service probably should be a rest api, but 90% of other microservices use grpc and don't expose any methods for public, all use gateway, so to make it consistent
i prefer consistency over logic. user-service will be grpc microservice, yes it's overkill, but zhizn' igra, igray krasivo


auth service

...


order service:
i am idiot so i admire complexity: saga based transaction with sprinkle of outbox with workers and 200 mg of kafka