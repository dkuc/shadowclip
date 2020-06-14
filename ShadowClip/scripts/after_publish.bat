 for %%f in (..\local_patches\*.patch) do (
	bash.exe -c "git apply -R ../local_patches/%%~nf%%~xf"       
 )
start bash.exe -c "scp -r bin/%2/%1/app.publish/* pi@pi4:/home/pi/data/nginx/html/shadowclip"
