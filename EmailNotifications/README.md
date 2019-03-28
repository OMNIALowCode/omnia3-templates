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

    string to = "";
    string subject = "";
    string body = "";
    
    // Send email
    SystemApplicationBehaviours.SendEmailNotification(
    new Dictionary<string, object>
        {
            {"Email", to}, 
            {"Subject", subject}, 
            {"Body", body}
        }, 
        context);

```

## Production scenarios

In production, scenarios are recommended to generate a new Encryption Key and change it in the _GetEncryptionKey_ Application Behaviour.