MRuby::CrossBuild.new('ios-arm64') do |conf|
  conf.gembox '../../../vitalrouter'

  sdk = '/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk/'

  conf.cc do |cc|
    cc.command = 'xcrun'
    cc.flags = %W(-sdk iphoneos clang -arch arm64 -isysroot "#{sdk}" -g -O3 -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk iphoneos clang -arch arm64 -isysroot "#{sdk}")
  end
end

MRuby::CrossBuild.new('ios-x64') do |conf|
  sdk = '/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneSimulator.platform/Developer/SDKs/iPhoneSimulator.sdk/'

  conf.cc do |cc|
    cc.command = 'xcrun'
    cc.flags = %W(-sdk iphonesimulator clang -arch x86_64 -isysroot "#{sdk}" -g -O3 -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk iphonesimulator clang -arch x86_64 -isysroot "#{sdk}")
  end

  conf.gembox '../../../vitalrouter'
end
