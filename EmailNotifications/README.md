# Email Notifications Micro Template

Template to add the required model to start sending emails using an SMTP server.


## Getting started

 1. Import to your OMNIA tenant the template Zip;

> https://github.com/OMNIALowCode/omnia3-templates/blob/master/EmailNotifications/Template/EmailNotificationsTemplate.zip?raw=true

 2. Build & Deploy the model;
 3. Go to the Application and create a new instance of the Settings entity, defining the SMTP settings to use;


## Sending notifications

To send notifications, simply add the following code block to the behaviour where you want to trigger the notification.

```C#

    //Set as true to send email using a custom smtp server. Set as false to use Omnia default smtp server.
    var usingExternalSmtpServer = false;

    var sendTo = "";
    var subjectTextTemplate = "";
    var bodyTextTemplate = "";

    //Dictionary with data to be used on email composition. Can be set manually or by transforming the object into Dto
    Dictionary<string, object> dto = this.ToDto();
    
    // Send email
    SystemApplicationBehaviours.SendEmailNotification(
    new Dictionary<string, object>
        {
            //UsingExternalSmtpServer is an optional parameter. If not sent, default value will be assumed as false
            {"UsingExternalSmtpServer", usingExternalSmtpServer}, 
            {"SendTo", sendTo}, 
            {"SubjectTextTemplate", subjectTextTemplate}, 
            {"BodyTextTemplate", bodyTextTemplate},
            {"Dto", dto}
        }, 
        context);

```

## Production scenarios

In production, scenarios are recommended to generate a new Encryption Key and change it in the _GetEncryptionKey_ Application Behaviour.