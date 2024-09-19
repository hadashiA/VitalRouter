MRuby::CrossBuild.new('tvos-arm64') do |conf|
  sdk = `xcrun -sdk appletvos --show-sdk-path`.chomp

  conf.cc do |cc|
    cc.command = 'xcrun'
    cc.flags = %W(-sdk appletvos clang -arch arm64 -isysroot "#{sdk}" -g -O3 -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk appletvos clang -arch arm64 -isysroot "#{sdk}")
  end

  conf.gembox '../../../vitalrouter'
end

MRuby::CrossBuild.new('tvos-x64') do |conf|
  sdk = `xcrun -sdk appletvos --show-sdk-path`.chomp  
  
  conf.cc do |cc|
    cc.command = 'xcrun'
    cc.flags = %W(-sdk appletvsimulator clang -arch x86_64 -isysroot "#{sdk}" -g -O3 -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk appletvsimulator clang -arch x86_64 -isysroot "#{sdk}")
  end

  conf.gembox '../../../vitalrouter'
end
