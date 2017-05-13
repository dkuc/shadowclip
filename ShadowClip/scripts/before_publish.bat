
for %%f in (..\local_patches\*.patch) do (
	bash.exe -c "git apply  ../local_patches/%%~nf%%~xf"       
)

