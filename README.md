# SignitAPI.Client

To configure client you have to add several settings in web.config:

```xml
 <!--Signit-->
    <add key="SignitMerchantName" value="" />
    <add key="SignitMerchantPassword" value="" />
    <!--https://www.signplatform.com for production-->
    <add key="SignitApiBaseUri" value="https://pp.signplatform.com" />
	<!--URI used when your signature window has finished the signature process-->
    <add key="SignitApiExitUrl" value="http://localhost:1742/cart#digital-signature/" />
    <!--Your obtained client id. You can register on https://pp.signplatform.com as a company and get client id and secret-->
    <!--Keep in mind that you need to contact signit to enable API-->
    <add key="SignitClientId" value="" />
    <add key="SignitClientSecret" value="" />
<!--Signit end-->
```

Here if you provide merchant name and password, you will use signit on behalf of this specific user.

You can also obtain Bearer Token using oAuth2 flow via 
https://pp.signplatform.com/oAuth/Authorize 
and 
https://pp.signplatform.com/Token endpoints. 
Client allows you to provide your own Token. To enable API, please contact Signit.dk support.
