# Acceptance tests

Because of the nature of this software there are no predefined automatic
acceptance tests that would automatically delete items. In theory, this could
be done if this software had an separate subscription, but currently this has
not been a priority.

Howoever, there are some scripts that make manual acceptace testing easier.

*IMPORTANT*: Following scripts use `developer-settings.json` as source of
settings (name of resource group, etc), so please verify that this file
contains correct parameters BEFORE executing this scripts.

*IMPORTANT*: As this really DELETES resources, please verify that you deploy
this software to correct subscription.

1. `deployment/Prepare-Environment.ps1` This creates relevant resources and
deploys this software to Azure.
1. `testing/GenerateDeletableResource.ps1` creates a specified amount of empty
resource groups which can be deleted. Please note that deleting a large number
of resource groups can be very slow, even if there is no content.
1. `testing/TriggerFunction.ps1` This triggers the delete function, because
normally the deletion would be triggered only once in a month.
1. `testing/Login.ps1` This can be used for service principal login if
necessary.
