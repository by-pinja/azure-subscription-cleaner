# How to opt out

This software doesn't delete items that are locked in Azure, either by delete
or read only lock. Easiest way to create locks is from Azure Portal, but
PowerShell or ARM-templates can also be used.

However, locks should be used sparingly and not in place of missing
infrastructure scripts.

Read more about locks from [Microsoft](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/lock-resources)
