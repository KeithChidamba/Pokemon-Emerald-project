sh BuildPokemonProj.sh
sh TemplateReplacer.sh 
sh GitScriptForBuild.sh
if [ $? -eq 0 ]; then
  echo "✅ Build succeeded!"
else
  echo "❌ Build failed. Check build.log"
fi
