# CPIService
Please write a web service in C# that provides the CPI value (as integer)  and notes (text) for a given month and year. 

 

Your service will call the CPI service and get the real data, then cache it for any additional calls so our application doesn’t call the real service more than their allowable limit.

 

Here is the public version of the site: https://www.bls.gov/developers/api_signature.htm

 

We use v1 of the web service; however, you may use v2 if you wish.

 

Example: https://api.bls.gov/publicAPI/v1/timeseries/data/LAUCN040010000000005

 

The CPI website is a public web service that has a limit on the number of times any given IP range can call it. (I think the limit is 25).

 

For instance, if I call the web service by providing “May 2020” as the input, it will return an int with the CPI along with any notes.

 

For that reason, your Web service will call the real CPI web service and cache the results for 1 day.  You can imagine a service like this allowing our application to get CPI information as needed, without exceeding the 25-call limit imposed by the real service.

 

Please create the service and email us the executable as well as a compressed file(ex: zip file) of your code.