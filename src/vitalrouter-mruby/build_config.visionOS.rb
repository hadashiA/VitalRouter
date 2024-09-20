MRuby::CrossBuild.new('visionos-arm64') do |conf|
  conf.gembox '../../../vitalrouter'

  sdk = `xcrun -sdk xros --show-sdk-path`.chomp  

  conf.cc do |cc|
    cc.defines << 'MRB_NO_BOXING'
    cc.command = 'xcrun'
    cc.flags = %W(-sdk xros clang -arch arm64 -isysroot "#{sdk}" -g -O3 -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk xros clang -arch arm64 -isysroot "#{sdk}")
  end
end

MRuby::CrossBuild.new('visionos-x64') do |conf|
  sdk = `xcrun -sdk xrsimulator --show-sdk-path`.chomp  

  conf.cc do |cc|
    cc.defines << 'MRB_NO_BOXING'    
    cc.command = 'xcrun'
    cc.flags = %W(-sdk xrsimulator clang -arch x86_64 -isysroot "#{sdk}" -g -O3 -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk xrsimulator clang -arch x86_64 -isysroot "#{sdk}")
  end

  conf.gembox '../../../vitalrouter'
end
