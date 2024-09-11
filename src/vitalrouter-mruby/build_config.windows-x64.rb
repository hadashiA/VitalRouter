MRuby::CrossBuild.new('windows-x64') do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'
end
