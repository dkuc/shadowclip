 for %%f in (..\local_patches\*.patch) do (
	bash.exe -c "git apply -R ../local_patches/%%~nf%%~xf"       
 )
start bash.exe -c "scp -r bin/%2/%1/app.publish/* pi@192.168.1.69:/usr/local/nginx/html/shadowclip"
