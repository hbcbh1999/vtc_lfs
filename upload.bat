:: Needs 7z (7zip) and aws-cli to be installed and configured with credentials (for aws).
call 7z a -tzip "VTC Installer.zip" "Installer.msi"
echo "Done zipping"
call aws s3 cp "VTC Installer.zip" "s3://traffic-camera.vtc/"
echo "Done uploading"
call aws s3api put-object-acl --bucket traffic-camera.vtc --key "VTC Installer.zip" --grant-read uri=http://acs.amazonaws.com/groups/global/AllUsers
echo "Done updating permissions"